use proc_macro::TokenStream;
use quote::quote;
use syn::{parse_macro_input, DeriveInput, Lit, Meta, MetaNameValue};

#[proc_macro_derive(AxonObject, attributes(type_id))]
pub fn axon_object_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let type_id = match extract_u32_attr(&input, "type_id") {
        Some(id) => id,
        None => {
            return syn::Error::new_spanned(
                name,
                "RpcObjectType requires #[type_id = <u32>] attribute",
            )
            .to_compile_error()
            .into();
        }
    };

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonObject for #name #ty_generics #where_clause {
            fn axon_object_type() -> u32 {
                #type_id
            }
        }
    };

    TokenStream::from(expanded)
}

#[proc_macro_derive(AxonVariant, attributes(type_id))]
pub fn axon_variant_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let type_id = match extract_u32_attr(&input, "type_id") {
        Some(id) => id,
        None => {
            return syn::Error::new_spanned(
                name,
                "RpcObjectType requires #[type_id = <u32>] attribute",
            )
            .to_compile_error()
            .into();
        }
    };

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonVariant for #name #ty_generics #where_clause {
            fn axon_variant_type() -> u32 {
                #type_id
            }
        }
    };

    TokenStream::from(expanded)
}

#[proc_macro_derive(AxonEvent, attributes(type_id))]
pub fn axon_event_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let type_id = match extract_u32_attr(&input, "type_id") {
        Some(id) => id,
        None => {
            return syn::Error::new_spanned(
                name,
                "RpcObjectType requires #[type_id = <u32>] attribute",
            )
            .to_compile_error()
            .into();
        }
    };

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonEvent for #name #ty_generics #where_clause {
            fn axon_event_type() -> u32 {
                #type_id
            }
            fn axon_event_invoke(bytes: &[u8],  commands: &mut ::bevy::prelude::Commands<'_, '_>) {
                let event = ::serde_json::from_slice::<Self>(bytes).unwrap();
                commands.trigger(event);
            }
        }
    };

    TokenStream::from(expanded)
}

fn extract_u32_attr(input: &DeriveInput, attr_name: &str) -> Option<u32> {
    for attr in &input.attrs {
        if !attr.path().is_ident(attr_name) {
            continue;
        }

        let meta = attr.meta.clone();
        if let Meta::NameValue(MetaNameValue {
            value: syn::Expr::Lit(expr_lit),
            ..
        }) = meta
        {
            if let Lit::Int(lit_int) = &expr_lit.lit {
                return lit_int.base10_parse::<u32>().ok();
            }
        }
    }
    None
}
