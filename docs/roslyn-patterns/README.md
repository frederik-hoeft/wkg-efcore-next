# Roslyn Patterns

Reusable architectural patterns for Roslyn source generators. The patterns are independent but composable: use them together when a generator needs both a stable compile-time protocol and extensibility across referenced assemblies.

## Patterns

### [Canonical Embedded Source Introspection (CESI)](patterns/canonical-embedded-source-introspection/canonical-embedded-source-introspection.md)

Use CESI when a small source-level protocol must exist both inside the generator and in analyzer-consuming compilations. A single canonical C# declaration is compiled into the generator, embedded as source, introspected for its identity, and injected into consumers, **preventing drift between metadata names**, generator-side types, and emitted bootstrap source.

### [Compile-Time Contract Discovery (CTCD)](patterns/compile-time-contract-discovery/compile-time-contract-discovery.md)

Use CTCD for **dependency inversion** when generated code needs runtime- or extension-owned types but the generator should not directly depend on those assemblies. Participating assemblies register concrete types against stable semantic roles, and the generator discovers and validates those registrations through Roslyn symbols before emitting references to the resolved types.

### [Compile-Time Symbolic Binding (CTSB)](patterns/compile-time-symbolic-binding/compile-time-symbolic-binding.md)

Use CTSB when referenced assemblies need to contribute **conditional behavioral extensions to the generator** without analyzer-time plugin loading or separate provider generators. Extensions publish structural binding rules that select ordinary compiled operations; the generator resolves those rules against its generation model and lowers the selected symbols into concrete generated source.

## Samples

- [CESI + CTCD sample](samples/cesi+ctcd-patterns/README.md)
