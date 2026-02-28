use serde::{Deserialize, Serialize};
use std::fs;
use std::path::{Path, PathBuf};
use syn::{parse_file, Attribute, Item, Meta};

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Metadata {
    pub o: Vec<Info>,
    pub v: Vec<Info>,
    pub e: Vec<Info>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct Info {
    pub i: u32,
    pub n: String,
    pub p: Vec<FieldInfo>,
}

#[derive(Serialize, Deserialize, Debug, Clone)]
pub struct FieldInfo {
    pub n: String,
    pub t: String,
    pub p: Vec<FieldInfo>,
}

pub fn run(src: &str, dst: &str) {
    let src_path = Path::new(src);

    if !src_path.exists() {
        eprintln!("Error: Source directory '{}' does not exist", src);
        std::process::exit(1);
    }

    if !src_path.is_dir() {
        eprintln!("Error: '{}' is not a directory", src);
        std::process::exit(1);
    }

    // Find project root and read crate name from Cargo.toml
    let (crate_name, project_root) = find_crate_info(src_path);

    let mut metadata = Metadata {
        o: Vec::new(),
        v: Vec::new(),
        e: Vec::new(),
    };

    if let Err(e) = collect_files(src_path, &mut metadata, &crate_name, &project_root) {
        eprintln!("Error collecting files: {}", e);
        std::process::exit(1);
    }

    match serde_json::to_string_pretty(&metadata) {
        Ok(json) => {
            if let Err(e) = fs::write(dst, json) {
                eprintln!("Error writing output file '{}': {}", dst, e);
                std::process::exit(1);
            }
            println!("Metadata extracted successfully to '{}'", dst);
            println!("  - {} AxonObject found", metadata.o.len());
            println!("  - {} AxonVariant found", metadata.v.len());
            println!("  - {} AxonEvent found", metadata.e.len());
        }
        Err(e) => {
            eprintln!("Error serializing metadata: {}", e);
            std::process::exit(1);
        }
    }
}

fn find_crate_info(start_path: &Path) -> (String, PathBuf) {
    let mut current = start_path;
    loop {
        let cargo_toml = current.join("Cargo.toml");
        if cargo_toml.exists() {
            if let Ok(content) = fs::read_to_string(&cargo_toml) {
                if let Some(name) = parse_cargo_toml_name(&content) {
                    return (name, current.to_path_buf());
                }
            }
        }
        match current.parent() {
            Some(parent) => current = parent,
            None => break,
        }
    }
    ("unknown".to_string(), start_path.to_path_buf())
}

fn parse_cargo_toml_name(content: &str) -> Option<String> {
    for line in content.lines() {
        let line = line.trim();
        if line.starts_with("name") && line.contains('=') {
            let parts: Vec<&str> = line.split('=').collect();
            if parts.len() >= 2 {
                let name = parts[1].trim().trim_matches('"').trim_matches('\'');
                return Some(name.to_string());
            }
        }
    }
    None
}

fn collect_files(
    dir: &Path,
    metadata: &mut Metadata,
    crate_name: &str,
    project_root: &Path,
) -> Result<(), Box<dyn std::error::Error>> {
    for entry in fs::read_dir(dir)? {
        let entry = entry?;
        let path = entry.path();
        let metadata_entry = entry.metadata()?;

        if metadata_entry.is_file() {
            if path.extension().map(|e| e == "rs").unwrap_or(false) {
                if let Ok(content) = fs::read_to_string(&path) {
                    let module_path = build_module_path(&path, project_root, crate_name);
                    parse_rust_file(&content, metadata, &module_path);
                }
            }
        } else if metadata_entry.is_dir() {
            collect_files(&path, metadata, crate_name, project_root)?;
        }
    }

    Ok(())
}

fn build_module_path(file_path: &Path, project_root: &Path, crate_name: &str) -> String {
    let src_dir = project_root.join("src");
    if let Ok(relative) = file_path.strip_prefix(&src_dir) {
        let mut parts: Vec<String> = vec![crate_name.to_string()];

        // Get path components (excluding the file name)
        let parent = relative.parent();
        if let Some(parent) = parent {
            for component in parent.components() {
                if let Some(s) = component.as_os_str().to_str() {
                    parts.push(s.to_string());
                }
            }
        }

        // Get file stem (without .rs extension)
        if let Some(stem) = file_path.file_stem() {
            if let Some(s) = stem.to_str() {
                // Skip "mod" or "lib" or "main" as they represent the module root
                if s != "mod" && s != "lib" && s != "main" {
                    parts.push(s.to_string());
                }
            }
        }

        parts.join("::")
    } else {
        crate_name.to_string()
    }
}

fn parse_rust_file(content: &str, metadata: &mut Metadata, module_path: &str) {
    let file = match parse_file(content) {
        Ok(f) => f,
        Err(_) => return,
    };

    let items: Vec<_> = file.items.iter().collect();

    for item in &file.items {
        if let Item::Struct(item_struct) = item {
            let struct_name = item_struct.ident.to_string();

            let has_axon_object = has_derive(&item_struct.attrs, "AxonObject");
            let has_axon_variant = has_derive(&item_struct.attrs, "AxonVariant");
            let has_axon_event = has_derive(&item_struct.attrs, "AxonEvent");

            if !has_axon_object && !has_axon_variant && !has_axon_event {
                continue;
            }

            // Build full qualified name: crate_name::module::StructName
            let full_name = if module_path.is_empty() {
                struct_name.clone()
            } else {
                format!("{}::{}", module_path, struct_name)
            };

            let type_id = compute_type_id(&full_name);
            let fields = extract_fields(&item_struct.fields, &items);

            let info = Info {
                i: type_id,
                n: full_name,
                p: fields,
            };

            if has_axon_object {
                metadata.o.push(info.clone());
            }
            if has_axon_variant {
                metadata.v.push(info.clone());
            }
            if has_axon_event {
                metadata.e.push(info.clone());
            }
        }
    }
}

fn has_derive(attrs: &[Attribute], derive_name: &str) -> bool {
    for attr in attrs {
        if attr.path().is_ident("derive") {
            if let Meta::List(meta_list) = &attr.meta {
                let nested = meta_list.tokens.clone();
                let derives: Vec<String> = nested
                    .to_string()
                    .split(',')
                    .map(|s| s.trim().to_string())
                    .collect();
                for d in derives {
                    if d == derive_name {
                        return true;
                    }
                }
            }
        }
    }
    false
}

fn compute_type_id(name: &str) -> u32 {
    let bytes = name.as_bytes();
    let mut hash: u32 = 5381;
    let mut i = 0;
    while i < bytes.len() {
        hash = hash.wrapping_mul(33).wrapping_add(bytes[i] as u32);
        i += 1;
    }
    hash
}

fn extract_fields(fields: &syn::Fields, all_items: &[&Item]) -> Vec<FieldInfo> {
    let mut result = Vec::new();

    match fields {
        syn::Fields::Named(named) => {
            for field in &named.named {
                let name = field
                    .ident
                    .as_ref()
                    .map(|i| i.to_string())
                    .unwrap_or_default();
                let (ty, nested_fields) = format_type(&field.ty, all_items);
                result.push(FieldInfo {
                    n: name,
                    t: ty,
                    p: nested_fields,
                });
            }
        }
        syn::Fields::Unnamed(unnamed) => {
            for (i, field) in unnamed.unnamed.iter().enumerate() {
                let name = format!("_{}", i);
                let (ty, nested_fields) = format_type(&field.ty, all_items);
                result.push(FieldInfo {
                    n: name,
                    t: ty,
                    p: nested_fields,
                });
            }
        }
        syn::Fields::Unit => {}
    }

    result
}

fn format_type(ty: &syn::Type, all_items: &[&Item]) -> (String, Vec<FieldInfo>) {
    match ty {
        syn::Type::Path(type_path) => {
            let path = &type_path.path;
            let type_name = path
                .segments
                .last()
                .map(|s| s.ident.to_string())
                .unwrap_or_default();

            let last_segment = path.segments.last();

            if let Some(segment) = last_segment {
                if let syn::PathArguments::AngleBracketed(args) = &segment.arguments {
                    let inner_types: Vec<String> = args
                        .args
                        .iter()
                        .filter_map(|arg| {
                            if let syn::GenericArgument::Type(inner_ty) = arg {
                                Some(get_type_name(inner_ty))
                            } else {
                                None
                            }
                        })
                        .collect();

                    if type_name == "Vec" && inner_types.len() == 1 {
                        let inner_type_name = &inner_types[0];
                        let nested_fields = find_struct_fields(inner_type_name, all_items);
                        return (format!("{}[]", inner_type_name), nested_fields);
                    } else if type_name == "Option" && inner_types.len() == 1 {
                        let inner_type_name = &inner_types[0];
                        let nested_fields = find_struct_fields(inner_type_name, all_items);
                        return (format!("{}?", inner_type_name), nested_fields);
                    } else {
                        let full_type = format!("{}<{}>", type_name, inner_types.join(", "));
                        return (full_type, Vec::new());
                    }
                }
            }

            let nested_fields = find_struct_fields(&type_name, all_items);
            (type_name, nested_fields)
        }
        syn::Type::Array(type_array) => {
            let (inner_type, nested_fields) = format_type(&type_array.elem, all_items);
            let len = &type_array.len;
            (
                format!("{}[{}]", inner_type, quote::quote!(#len)),
                nested_fields,
            )
        }
        syn::Type::Tuple(type_tuple) => {
            let types: Vec<String> = type_tuple.elems.iter().map(|t| get_type_name(t)).collect();
            (format!("({})", types.join(", ")), Vec::new())
        }
        syn::Type::Reference(type_ref) => {
            let (inner_type, nested_fields) = format_type(&type_ref.elem, all_items);
            if type_ref.mutability.is_some() {
                (format!("&mut {}", inner_type), nested_fields)
            } else {
                (format!("&{}", inner_type), nested_fields)
            }
        }
        _ => {
            let type_str = quote::quote!(#ty).to_string();
            (type_str, Vec::new())
        }
    }
}

fn get_type_name(ty: &syn::Type) -> String {
    match ty {
        syn::Type::Path(type_path) => type_path
            .path
            .segments
            .last()
            .map(|s| {
                let name = s.ident.to_string();
                if let syn::PathArguments::AngleBracketed(args) = &s.arguments {
                    let inner: Vec<String> = args
                        .args
                        .iter()
                        .filter_map(|arg| {
                            if let syn::GenericArgument::Type(inner_ty) = arg {
                                Some(get_type_name(inner_ty))
                            } else {
                                None
                            }
                        })
                        .collect();
                    format!("{}<{}>", name, inner.join(", "))
                } else {
                    name
                }
            })
            .unwrap_or_default(),
        _ => quote::quote!(#ty).to_string(),
    }
}

fn find_struct_fields(type_name: &str, all_items: &[&Item]) -> Vec<FieldInfo> {
    for item in all_items {
        if let Item::Struct(item_struct) = item {
            if item_struct.ident.to_string() == type_name {
                return extract_fields(&item_struct.fields, all_items);
            }
        }
    }
    Vec::new()
}
