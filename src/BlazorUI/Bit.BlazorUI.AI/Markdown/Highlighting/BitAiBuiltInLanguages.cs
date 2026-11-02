namespace Bit.BlazorUI;

/// <summary>
/// The languages registered by default.
/// </summary>
/// <remarks>
/// Chosen by what models actually write in chat: application languages, the data formats around
/// them, and the shell. Anything else falls back to plain text, which renders correctly and simply
/// is not coloured.
/// </remarks>
public static class BitAiBuiltInLanguages
{
    private static HashSet<string> Set(params string[] words) => new(words, StringComparer.Ordinal);

    /// <summary>C# and its aliases.</summary>
    public static BitAiLanguageDefinition CSharp { get; } = new("csharp")
    {
        Aliases = ["cs", "c#", "dotnet"],
        Keywords = Set(
            "abstract", "as", "async", "await", "base", "break", "case", "catch", "checked", "class",
            "const", "continue", "default", "delegate", "do", "else", "enum", "event", "explicit",
            "extern", "file", "finally", "fixed", "for", "foreach", "get", "goto", "if", "implicit",
            "in", "init", "interface", "internal", "is", "lock", "namespace", "new", "operator",
            "out", "override", "params", "partial", "private", "protected", "public", "readonly",
            "record", "ref", "required", "return", "sealed", "set", "sizeof", "stackalloc", "static",
            "struct", "switch", "this", "throw", "try", "typeof", "unchecked", "unsafe", "using",
            "virtual", "volatile", "when", "where", "while", "with", "yield"),
        Types = Set(
            "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "nint",
            "nuint", "object", "sbyte", "short", "string", "uint", "ulong", "ushort", "var", "void",
            "Task", "ValueTask", "List", "Dictionary", "IEnumerable", "IAsyncEnumerable", "Span"),
        Constants = Set("true", "false", "null", "default"),
        MultiLineStrings = true,
        AnnotationPrefix = '[',
    };

    /// <summary>JavaScript and TypeScript.</summary>
    public static BitAiLanguageDefinition JavaScript { get; } = new("javascript")
    {
        Aliases = ["js", "jsx", "typescript", "ts", "tsx", "mjs", "cjs"],
        Keywords = Set(
            "as", "async", "await", "break", "case", "catch", "class", "const", "continue",
            "debugger", "declare", "default", "delete", "do", "else", "enum", "export", "extends",
            "finally", "for", "from", "function", "get", "if", "implements", "import", "in",
            "instanceof", "interface", "let", "new", "of", "private", "protected", "public",
            "readonly", "return", "satisfies", "set", "static", "super", "switch", "this", "throw",
            "try", "type", "typeof", "var", "void", "while", "yield"),
        Types = Set(
            "any", "bigint", "boolean", "never", "number", "object", "string", "symbol", "unknown",
            "Array", "Map", "Promise", "Record", "Set", "Date", "RegExp", "Error"),
        Constants = Set("true", "false", "null", "undefined", "NaN", "Infinity"),
        StringDelimiters = ['"', '\'', '`'],
    };

    /// <summary>Python.</summary>
    public static BitAiLanguageDefinition Python { get; } = new("python")
    {
        Aliases = ["py", "python3"],
        Keywords = Set(
            "and", "as", "assert", "async", "await", "break", "class", "continue", "def", "del",
            "elif", "else", "except", "finally", "for", "from", "global", "if", "import", "in",
            "is", "lambda", "match", "nonlocal", "not", "or", "pass", "raise", "return", "try",
            "while", "with", "yield"),
        Types = Set("bool", "bytes", "dict", "float", "frozenset", "int", "list", "set", "str", "tuple", "type"),
        Constants = Set("True", "False", "None", "self", "cls"),
        LineComments = ["#"],
        BlockComments = [],
        AnnotationPrefix = '@',
    };

    /// <summary>JSON.</summary>
    public static BitAiLanguageDefinition Json { get; } = new("json")
    {
        Aliases = ["jsonc", "json5"],
        Constants = Set("true", "false", "null"),
        LineComments = ["//"],
        StringDelimiters = ['"'],
        ColonProperties = true,
    };

    /// <summary>HTML and XML.</summary>
    public static BitAiLanguageDefinition Html { get; } = new("html")
    {
        Aliases = ["xml", "svg", "xhtml", "razor", "cshtml", "vue"],
        LineComments = [],
        BlockComments = [("<!--", "-->")],
        IsMarkup = true,
    };

    /// <summary>CSS and its preprocessors.</summary>
    public static BitAiLanguageDefinition Css { get; } = new("css")
    {
        Aliases = ["scss", "sass", "less"],
        Keywords = Set("important", "media", "import", "keyframes", "supports", "layer", "container"),
        Constants = Set("inherit", "initial", "unset", "none", "auto", "currentColor", "transparent"),
        ColonProperties = true,
        UrlFunctions = true,
    };

    /// <summary>SQL.</summary>
    public static BitAiLanguageDefinition Sql { get; } = new("sql")
    {
        Aliases = ["postgresql", "mysql", "tsql", "sqlite", "plsql"],
        Keywords = Set(
            "select", "from", "where", "insert", "into", "values", "update", "set", "delete",
            "create", "alter", "drop", "table", "index", "view", "join", "inner", "left", "right",
            "full", "outer", "on", "group", "by", "order", "having", "limit", "offset", "union",
            "all", "distinct", "as", "and", "or", "not", "in", "exists", "between", "like", "case",
            "when", "then", "else", "end", "with", "returning", "primary", "foreign", "key",
            "references", "constraint", "default", "cascade", "begin", "commit", "rollback"),
        Types = Set("int", "integer", "bigint", "text", "varchar", "boolean", "timestamp", "date", "numeric", "uuid", "jsonb"),
        Constants = Set("null", "true", "false"),
        LineComments = ["--"],
        IgnoreCase = true,
    };

    /// <summary>Shell scripts.</summary>
    public static BitAiLanguageDefinition Shell { get; } = new("bash")
    {
        Aliases = ["sh", "shell", "zsh", "console"],
        Keywords = Set(
            "if", "then", "else", "elif", "fi", "for", "while", "until", "do", "done", "case",
            "esac", "function", "return", "export", "local", "source", "alias", "set", "unset",
            "echo", "cd", "exit", "trap", "shift", "read", "declare", "readonly"),
        Constants = Set("true", "false"),
        LineComments = ["#"],
        BlockComments = [],
        StringDelimiters = ['"', '\''],
    };

    /// <summary>PowerShell.</summary>
    /// <remarks>
    /// Its own grammar rather than an alias of the shell: cmdlets are <c>Verb-Noun</c>, parameters
    /// and comparison operators start with a hyphen, variables with a dollar sign, strings escape
    /// with a backtick, and <c>&lt;# #&gt;</c> is a block comment.
    /// </remarks>
    public static BitAiLanguageDefinition PowerShell { get; } = new("powershell")
    {
        Aliases = ["ps1", "pwsh", "ps", "posh"],
        Keywords = Set(
            "begin", "break", "catch", "class", "continue", "data", "define", "do", "dynamicparam",
            "else", "elseif", "end", "enum", "exit", "filter", "finally", "for", "foreach", "from",
            "function", "hidden", "if", "in", "inlinescript", "param", "process", "return", "static",
            "switch", "throw", "trap", "try", "until", "using", "var", "while", "workflow",
            "-eq", "-ne", "-gt", "-ge", "-lt", "-le", "-like", "-notlike", "-match", "-notmatch",
            "-contains", "-notcontains", "-in", "-notin", "-replace", "-split", "-join", "-and",
            "-or", "-not", "-xor", "-is", "-isnot", "-as", "-band", "-bor", "-bxor", "-bnot",
            "-shl", "-shr", "-f", "-ceq", "-ieq", "-cne", "-ine", "-clike", "-ilike", "-cmatch", "-imatch"),
        Types = Set(
            "string", "int", "long", "bool", "double", "decimal", "char", "byte", "array", "hashtable",
            "pscustomobject", "psobject", "datetime", "timespan", "guid", "switch", "object", "void",
            "scriptblock", "regex", "xml", "uri", "version", "ordered"),
        Constants = Set("true", "false", "null"),
        LineComments = ["#"],
        BlockComments = [("<#", "#>")],
        StringDelimiters = ['"', '\''],
        StringEscapeCharacter = '`',
        MultiLineStrings = true,
        IgnoreCase = true,
        AnnotationPrefix = '[',
        VariablePrefix = '$',
        HyphenatedCommands = true,
    };

    /// <summary>Go.</summary>
    public static BitAiLanguageDefinition Go { get; } = new("go")
    {
        Aliases = ["golang"],
        Keywords = Set(
            "break", "case", "chan", "const", "continue", "default", "defer", "else", "fallthrough",
            "for", "func", "go", "goto", "if", "import", "interface", "map", "package", "range",
            "return", "select", "struct", "switch", "type", "var"),
        Types = Set(
            "bool", "byte", "complex64", "complex128", "error", "float32", "float64", "int", "int8",
            "int16", "int32", "int64", "rune", "string", "uint", "uint8", "uint16", "uint32",
            "uint64", "uintptr", "any"),
        Constants = Set("true", "false", "nil", "iota"),
        StringDelimiters = ['"', '`', '\''],
    };

    /// <summary>Rust.</summary>
    public static BitAiLanguageDefinition Rust { get; } = new("rust")
    {
        Aliases = ["rs"],
        Keywords = Set(
            "as", "async", "await", "break", "const", "continue", "crate", "dyn", "else", "enum",
            "extern", "fn", "for", "if", "impl", "in", "let", "loop", "match", "mod", "move", "mut",
            "pub", "ref", "return", "self", "static", "struct", "super", "trait", "type", "unsafe",
            "use", "where", "while"),
        Types = Set(
            "bool", "char", "f32", "f64", "i8", "i16", "i32", "i64", "i128", "isize", "str",
            "String", "u8", "u16", "u32", "u64", "u128", "usize", "Vec", "Option", "Result", "Box"),
        Constants = Set("true", "false", "None", "Some", "Ok", "Err"),
        AnnotationPrefix = '#',
        Lifetimes = true,
    };

    /// <summary>Java and Kotlin.</summary>
    public static BitAiLanguageDefinition Java { get; } = new("java")
    {
        Aliases = ["kotlin", "kt", "groovy", "scala"],
        Keywords = Set(
            "abstract", "assert", "break", "case", "catch", "class", "const", "continue", "default",
            "do", "else", "enum", "extends", "final", "finally", "for", "if", "implements",
            "import", "instanceof", "interface", "native", "new", "package", "private", "protected",
            "public", "return", "static", "strictfp", "super", "switch", "synchronized", "this",
            "throw", "throws", "transient", "try", "val", "var", "volatile", "while", "fun", "when"),
        Types = Set(
            "boolean", "byte", "char", "double", "float", "int", "long", "short", "void", "String",
            "Integer", "Boolean", "List", "Map", "Set", "Optional"),
        Constants = Set("true", "false", "null"),
        AnnotationPrefix = '@',
    };

    /// <summary>YAML.</summary>
    public static BitAiLanguageDefinition Yaml { get; } = new("yaml")
    {
        Aliases = ["yml"],
        Constants = Set("true", "false", "null", "yes", "no", "on", "off"),
        LineComments = ["#"],
        BlockComments = [],
        ColonProperties = true,
    };

    /// <summary>TOML and INI.</summary>
    public static BitAiLanguageDefinition Toml { get; } = new("toml")
    {
        Aliases = ["ini", "cfg", "conf", "properties", "editorconfig"],
        Constants = Set("true", "false"),
        LineComments = ["#", ";"],
        BlockComments = [],
        ColonProperties = true,
    };

    /// <summary>Diff and patch output.</summary>
    public static BitAiLanguageDefinition Diff { get; } = new("diff")
    {
        Aliases = ["patch"],
        LineComments = [],
        BlockComments = [],
        StringDelimiters = [],
    };

    /// <summary>Every built-in language.</summary>
    public static IReadOnlyList<BitAiLanguageDefinition> All { get; } =
    [
        CSharp, JavaScript, Python, Json, Html, Css, Sql, Shell, PowerShell, Go, Rust, Java, Yaml, Toml, Diff,
    ];
}
