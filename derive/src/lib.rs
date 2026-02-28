use proc_macro::TokenStream;
use quote::quote;
use syn::parse_macro_input;

#[proc_macro_derive(AxonObject)]
pub fn axon_object_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonObject for #name #ty_generics #where_clause {
            fn axon_object_type() -> u32 {
                const fn const_hash(s: &str) -> u32 {
                    let bytes = s.as_bytes();
                    let mut hash: u32 = 5381;
                    let mut i = 0;
                    while i < bytes.len() {
                        hash = hash.wrapping_mul(33).wrapping_add(bytes[i] as u32);
                        i += 1;
                    }
                    hash
                }
                const FULL_NAME: &str = concat!(module_path!(), "::", stringify!(#name));
                const HASH: u32 = const_hash(FULL_NAME);
                HASH
            }
        }
    };

    TokenStream::from(expanded)
}

#[proc_macro_derive(AxonVariant)]
pub fn axon_variant_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonVariant for #name #ty_generics #where_clause {
            fn axon_variant_type() -> u32 {
                const fn const_hash(s: &str) -> u32 {
                    let bytes = s.as_bytes();
                    let mut hash: u32 = 5381;
                    let mut i = 0;
                    while i < bytes.len() {
                        hash = hash.wrapping_mul(33).wrapping_add(bytes[i] as u32);
                        i += 1;
                    }
                    hash
                }
                const FULL_NAME: &str = concat!(module_path!(), "::", stringify!(#name));
                const HASH: u32 = const_hash(FULL_NAME);
                HASH
            }
        }
    };

    TokenStream::from(expanded)
}

#[proc_macro_derive(AxonEvent)]
pub fn axon_event_derive(input: TokenStream) -> TokenStream {
    let input = parse_macro_input!(input as syn::DeriveInput);
    let name = &input.ident;
    let (impl_generics, ty_generics, where_clause) = &input.generics.split_for_impl();

    let expanded = quote! {
        impl #impl_generics ::bevy_axon::core::AxonEvent for #name #ty_generics #where_clause {
            fn axon_event_type() -> u32 {
                const fn const_hash(s: &str) -> u32 {
                    let bytes = s.as_bytes();
                    let mut hash: u32 = 5381;
                    let mut i = 0;
                    while i < bytes.len() {
                        hash = hash.wrapping_mul(33).wrapping_add(bytes[i] as u32);
                        i += 1;
                    }
                    hash
                }
                const FULL_NAME: &str = concat!(module_path!(), "::", stringify!(#name));
                const HASH: u32 = const_hash(FULL_NAME);
                HASH
            }
            fn axon_event_invoke(bytes: &[u8],  commands: &mut ::bevy::prelude::Commands<'_, '_>) {
                let event = ::serde_json::from_slice::<Self>(bytes).unwrap();
                commands.trigger(event);
            }
        }
    };

    TokenStream::from(expanded)
}
