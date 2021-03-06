using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Benutomo.AutomaticDisposeImpl.SourceGenerator
{
    [Generator]
    public partial class AutomaticDisposeGenerator : IIncrementalGenerator
    {
#if DEBUG
        static StreamWriter _streamWriter;
        static AutomaticDisposeGenerator()
        {
            Directory.CreateDirectory(@"c:\var\log\AutomaticDisposeGenerator");
            var proc = Process.GetCurrentProcess();
            _streamWriter = new StreamWriter($@"c:\var\log\AutomaticDisposeGenerator\{DateTime.Now:yyyyMMddHHmmss}_{proc.Id}.txt");
            _streamWriter.WriteLine(proc);
        }

        [Conditional("DEBUG")]
        static void WriteLogLine(string line)
        {
            lock (_streamWriter)
            {
                _streamWriter.WriteLine(line);
                _streamWriter.Flush();
            }
        }
#else
        [Conditional("DEBUG")]
        static void WriteLogLine(string line)
        {
        }
#endif



        internal const string AttributeDefinedNameSpace = "Benutomo";




        private const string AutomaticDisposeImplModeSource = @"
#pragma warning disable CS0436
namespace Benutomo
{
    /// <summary>
    /// 破棄(<see cref=""System.IDisposable"" />,<see cref=""System.IAsyncDisposable"" />)をサポートするメンバを自動実装Disposeの対象とすることに関する振る舞いの指定。
    /// </summary>
    internal enum AutomaticDisposeImplMode
    {
        /// <summary>
        /// <see cref=""System.IDisposable"" />,<see cref=""System.IAsyncDisposable"" />を継承する型を持つメンバは暗黙的に自動Dispose呼び出しの対象となる。
        /// </summary>
        Implicit,

        /// <summary>
        /// <see cref=""System.IDisposable"" />,<see cref=""System.IAsyncDisposable"" />を継承する型を持つメンバは自動Dispose呼び出しの対象となる。
        /// </summary>
        Explicit,
    }
}
";

        internal const string AutomaticDisposeImplAttributeCoreName = "AutomaticDisposeImpl";
        internal const string AutomaticDisposeImplAttributeName = "AutomaticDisposeImplAttribute";
        internal const string AutomaticDisposeImplAttributeFullyQualifiedMetadataName = "Benutomo.AutomaticDisposeImplAttribute";
        internal const string AutomaticDisposeImplAttributeModeName = "Mode";
        private const string AutomaticDisposeImplAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// 指定したクラスに破棄(<see cref=""System.IDisposable"" />,<see cref=""System.IAsyncDisposable"" />)をサポートするメンバを破棄する<see cref=""System.IDisposable.Dispose"" />メソッドおよび<see cref=""System.IAsyncDisposable.DisposeAsync"" />メソッド(当該クラスに<see cref=""System.IAsyncDisposable"" />インターフェイスが含まれている場合のみ)を自動実装する。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class AutomaticDisposeImplAttribute : Attribute
    {
        /// <summary>
        /// 自動破棄実装の既定動作を設定する。
        /// </summary>
        public AutomaticDisposeImplMode Mode { get; set; }
    }
}
";

        internal const string EnableAutomaticDisposeAttributeName = "EnableAutomaticDisposeAttribute";
        internal const string EnableAutomaticDisposeAttributeFullyQualifiedMetadataName = "Benutomo.EnableAutomaticDisposeAttribute";
        private const string EnableAutomaticDisposeAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// このオブジェクトの破棄と同時に自動的に<see cref=""System.IDisposable.Dispose"" />メソッドまたは<see cref=""System.IAsyncDisposable.DisposeAsync"" />メソッドを呼び出します。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class EnableAutomaticDisposeAttribute : Attribute
    {
        public EnableAutomaticDisposeAttribute() {}

        /// <summary>
        /// このオブジェクトの破棄と同時に自動的に<see cref=""System.IDisposable.Dispose"" />メソッドまたは<see cref=""System.IAsyncDisposable.DisposeAsync"" />メソッドを呼び出します。
        /// </summary>
        /// <param name=""linkedMembers"">このメンバの破棄に連動して破棄されるメンバ(ここで列挙されたメンバはEnable/DisableAutomaticDispose属性を省略可能)</param>
        public EnableAutomaticDisposeAttribute(params string[] dependencyMembers) {}
    }
}
";

        internal const string DisableAutomaticDisposeAttributeName = "DisableAutomaticDisposeAttribute";
        internal const string DisableAutomaticDisposeAttributeFullyQualifiedMetadataName = "Benutomo.DisableAutomaticDisposeAttribute";
        private const string DisableAutomaticDisposeAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// このメンバに対して、<see cref=""System.IDisposable.Dispose"" />メソッドまたは<see cref=""System.IAsyncDisposable.DisposeAsync"" />メソッドの自動呼出しは行いません。このオブジェクトで破棄するのが不適当であるかユーザ自身が<see cref=""System.IDisposable.Dispose"" />メソッドまたは<see cref=""System.IAsyncDisposable.DisposeAsync"" />メソッドの呼び出しを実装するメンバです。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class DisableAutomaticDisposeAttribute : Attribute
    {
    }
}
";

#if false
        internal const string AutomaticDisposeImplModeAttributeName = "AutomaticDisposeImplModeAttribute";
        internal const string AutomaticDisposeImplModeAttributeFullyQualifiedMetadataName = "Benutomo.AutomaticDisposeImplModeAttribute";
        private const string AutomaticDisposeImplModeAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// メンバの破棄の自動実装の設定を明示する属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class AutomaticDisposeImplModeAttribute : Attribute
    {
        /// <summary>
        /// 自動実装Disposeの対象とするか否かに関する設定。
        /// </summary>
        public AutomaticDisposeImplMode Mode { get; }

        /// <summary>
        /// メンバの破棄の自動実装の設定を明示する属性。
        /// </summary>
        /// <param name=""mode"">自動実装Disposeの対象とするか否かに関する設定。</param>
        public AutomaticDisposeImplModeAttribute(AutomaticDisposeImplMode mode)
        {
            Mode = mode;
        }
    }
}
";
#endif

        internal const string UnmanagedResourceReleaseMethodAttributeName = "UnmanagedResourceReleaseMethodAttribute";
        internal const string UnmanagedResourceReleaseMethodAttributeFullyQualifiedMetadataName = "Benutomo.UnmanagedResourceReleaseMethodAttribute";
        private const string UnmanagedResourceReleaseMethodAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// <see cref=""Benutomo.AutomaticDisposeImplAttribute""/>を利用しているクラスで、ユーザが実装するアンマネージドリソースの解放を行うメソッド(引数なしで戻り値はvoid)に付与する。このメソッドはこのオブジェクトのDispose()またはDisposeAsync()、デストラクタのいずれかが初めて実行された時に自動実装コードから呼び出される。この属性を付与したメソッドは、実装者の責任でGCのファイナライズスレッドから呼び出されても問題無いように実装しなければならないことに注意すること。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class UnmanagedResourceReleaseMethodAttribute : Attribute
    {
        /// <summary>
        /// <inheritdoc cref=""Benutomo.UnmanagedResourceReleaseMethodAttribute""/>
        /// </summary>
        public UnmanagedResourceReleaseMethodAttribute() { }
    }
}
";

        internal const string ManagedObjectDisposeMethodAttributeName = "ManagedObjectDisposeMethodAttribute";
        internal const string ManagedObjectDisposeMethodAttributeFullyQualifiedMetadataName = "Benutomo.ManagedObjectDisposeMethodAttribute";
        private const string ManagedObjectDisposeMethodAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// <see cref=""Benutomo.AutomaticDisposeImplAttribute""/>を利用しているクラスで、ユーザが実装するマネージドオブジェクトを同期的な処理による破棄を行うメソッドに付与する。このメソッドはデストラクタからは呼び出されない。デストラクタからも呼び出される必要がある場合は<see cref=""Benutomo.UnmanagedResourceReleaseMethodAttribute"">を使用すること。この属性を付与するメソッドは引数なしで戻り値はvoidである必要がある。このメソッドはこのオブジェクトのDispose()が初めて実行された時に自動実装コードから呼び出される。ただし、このメソッドを所有するクラスがIAsyncDisposableも実装していて、かつ、DisposeAsync()によってこのオブジェクトが破棄された場合は、この属性が付与されているメソッドは呼び出されず、<see cref=""Benutomo.ManagedObjectAsyncDisposeMethodAttribute"">が付与されているメソッドが呼び出される。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class ManagedObjectDisposeMethodAttribute : Attribute
    {
        /// <summary>
        /// <inheritdoc cref=""Benutomo.ManagedObjectDisposeMethodAttribute""/>
        /// </summary>
        public ManagedObjectDisposeMethodAttribute() { }
    }
}
";

        internal const string ManagedObjectAsyncDisposeMethodAttributeName = "ManagedObjectAsyncDisposeMethodAttribute";
        internal const string ManagedObjectAsyncDisposeMethodAttributeFullyQualifiedMetadataName = "Benutomo.ManagedObjectAsyncDisposeMethodAttribute";
        private const string ManagedObjectAsyncDisposeMethodAttributeSource = @"
using System;

#pragma warning disable CS0436
#nullable enable

namespace Benutomo
{
    /// <summary>
    /// <see cref=""Benutomo.AutomaticDisposeImplAttribute""/>を利用しているクラスで、ユーザが実装するマネージドオブジェクトを非同期的な処理による破棄を行うメソッドに付与する。このメソッドはデストラクタからは呼び出されない。デストラクタからも呼び出される必要がある場合はデストラクタで必要な処理を全て同期的に行うようにした上で<see cref=""Benutomo.UnmanagedResourceReleaseMethodAttribute"">を使用すること。この属性を付与するメソッドは引数なしで戻り値は<see cref=""System.Threading.ValueTask"" />などawait可能な型である必要がある。このメソッドはこのオブジェクトのDisposeAsync()が初めて実行された時に自動実装コードから呼び出される。ただし、このメソッドを所有するクラスがIDisposableも実装していて、かつ、Dispose()によってこのオブジェクトが破棄された場合は、この属性が付与されているメソッドは呼び出されず、<see cref=""Benutomo.ManagedObjectDisposeMethodAttribute"">が付与されているメソッドが呼び出される。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class ManagedObjectAsyncDisposeMethodAttribute : Attribute
    {
        /// <summary>
        /// <inheritdoc cref=""Benutomo.ManagedObjectAsyncDisposeMethodAttribute""/>
        /// </summary>
        public ManagedObjectAsyncDisposeMethodAttribute() { }
    }
}
";

        record struct UsingSymbols(
            INamedTypeSymbol AutomaticDisposeImplAttributeSymbol,
            INamedTypeSymbol EnableAutomaticDisposeAttributeSymbol,
            INamedTypeSymbol DisableAutomaticDisposeAttributeSymbol,
            INamedTypeSymbol UnmanagedResourceReleaseMethodAttributeSymbol,
            INamedTypeSymbol ManagedObjectDisposeMethodAttributeSymbol,
            INamedTypeSymbol ManagedObjectAsyncDisposeMethodAttributeSymbol,
            INamedTypeSymbol DisposableSymbol,
            INamedTypeSymbol AsyncDisposableSymbol
            );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            WriteLogLine("Begin Initialize");

            context.RegisterPostInitializationOutput(PostInitialization);

            var usingSymbols = context.CompilationProvider
                .Select((compilation, cancellationToken) =>
                {
                    WriteLogLine("Begin GetTypeByMetadataName");

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var automaticDisposeImplAttributeSymbol = compilation.GetTypeByMetadataName(AutomaticDisposeImplAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var enableAutomaticDisposeAttributeSymbol = compilation.GetTypeByMetadataName(EnableAutomaticDisposeAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var disableAutomaticDisposeAttributeSymbol = compilation.GetTypeByMetadataName(DisableAutomaticDisposeAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var unmanagedResourceReleaseMethodAttributeSymbol = compilation.GetTypeByMetadataName(UnmanagedResourceReleaseMethodAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var managedObjectDisposeMethodAttributeSymbol = compilation.GetTypeByMetadataName(ManagedObjectDisposeMethodAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var managedObjectAsyncDisposeMethodAttributeSymbol = compilation.GetTypeByMetadataName(ManagedObjectAsyncDisposeMethodAttributeFullyQualifiedMetadataName) ?? throw new InvalidOperationException();
                        var disposableSymbol = compilation.GetTypeByMetadataName("System.IDisposable") ?? throw new InvalidOperationException();
                        var asyncDisposableSymbol = compilation.GetTypeByMetadataName("System.IAsyncDisposable") ?? throw new InvalidOperationException();

                        return new UsingSymbols(
                            automaticDisposeImplAttributeSymbol,
                            enableAutomaticDisposeAttributeSymbol,
                            disableAutomaticDisposeAttributeSymbol,
                            unmanagedResourceReleaseMethodAttributeSymbol,
                            managedObjectDisposeMethodAttributeSymbol,
                            managedObjectAsyncDisposeMethodAttributeSymbol,
                            disposableSymbol,
                            asyncDisposableSymbol
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        WriteLogLine("Canceled GetTypeByMetadataName");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        WriteLogLine("Exception GetTypeByMetadataName");
                        WriteLogLine(ex.ToString());
                        throw;
                    }
                });

            // Where句を使用しない。
            // https://github.com/dotnet/roslyn/issues/57991
            // 今は、Where句を使用するとSource GeneratorがVSでインクリメンタルに実行されたときに
            // 対象のコードの状態や編集内容などによって突然内部状態が壊れて機能しなくなる問題がおきる。

            var anotatedClasses = context.SyntaxProvider
                .CreateSyntaxProvider(Predicate, Transform)
                //.Where(v => v is not null)
                .Combine(usingSymbols)
                .Select(PostTransform)
                ;//.Where(v => v is not null);

            context.RegisterSourceOutput(anotatedClasses, Generate);

            WriteLogLine("End Initialize");
        }

        void PostInitialization(IncrementalGeneratorPostInitializationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("AutomaticDisposeImplAttribute.cs", AutomaticDisposeImplAttributeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("AutomaticDisposeImplMode.cs", AutomaticDisposeImplModeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("EnableAutomaticDisposeAttribute.cs", EnableAutomaticDisposeAttributeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("DisableAutomaticDisposeAttribute.cs", DisableAutomaticDisposeAttributeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("UnmanagedResourceReleaseMethodAttribute.cs", UnmanagedResourceReleaseMethodAttributeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("ManagedObjectDisposeMethodAttribute.cs", ManagedObjectDisposeMethodAttributeSource);
            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource("ManagedObjectAsyncDisposeMethodAttribute.cs", ManagedObjectAsyncDisposeMethodAttributeSource);
        }


        bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
        {
            //WriteLogLine("Predicate");

            return node is ClassDeclarationSyntax
            {
                AttributeLists.Count: > 0
            };
        }

        INamedTypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            WriteLogLine("Begin Transform");
            try
            {
                var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

                if (!classDeclarationSyntax.Modifiers.Any(modifier => modifier.ValueText == "partial"))
                {
                    return null;
                }

                var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, cancellationToken) as INamedTypeSymbol;

                WriteLogLine($"End Transform ({namedTypeSymbol?.ContainingType?.Name}.{namedTypeSymbol?.Name})");

                return namedTypeSymbol;
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled Transform");
                throw;
            }
        }

        SourceBuildInputs? PostTransform((INamedTypeSymbol? Left, UsingSymbols Right) v, CancellationToken ct)
        {
            var namedTypeSymbol = v.Left;
            var usingSymbols = v.Right;

            if (namedTypeSymbol is null) return null;

            WriteLogLine($"Begin PostTransform ({namedTypeSymbol.Name})");

            try
            {
                var automaticDisposeImplAttributeData = namedTypeSymbol.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, usingSymbols.AutomaticDisposeImplAttributeSymbol));
                if (automaticDisposeImplAttributeData is null)
                {
                    return null;
                }

                if (!IsAssignableToIDisposable(namedTypeSymbol) && !IsAssignableToIAsyncDisposable(namedTypeSymbol))
                {
                    return null;
                }

                var result = new SourceBuildInputs(namedTypeSymbol, usingSymbols, automaticDisposeImplAttributeData);

                WriteLogLine($"End PostTransform ({namedTypeSymbol.Name})");

                return result;
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled PostTransform ({namedTypeSymbol.Name})");
                throw;
            }
            catch (Exception ex)
            {
                WriteLogLine($"Exception PostTransform ({namedTypeSymbol.Name})");
                WriteLogLine(ex.ToString());
                throw;
            }
        }

        void Generate(SourceProductionContext context, SourceBuildInputs? sourceBuildInputs)
        {
            if (sourceBuildInputs is null) return;

            WriteLogLine($"Begin Generate ({sourceBuildInputs.TargetTypeInfo.Name})");

            try
            {
                var sourceBuilder = new SourceBuilder(context, sourceBuildInputs);

                sourceBuilder.Build();

                context.AddSource(sourceBuilder.HintName, sourceBuilder.SourceText);

                WriteLogLine($"End Generate ({sourceBuildInputs.TargetTypeInfo.Name}) => {sourceBuilder.HintName}");
            }
            catch (OperationCanceledException)
            {
                WriteLogLine($"Canceled Generate ({sourceBuildInputs.TargetTypeInfo.Name})");
                throw;
            }
            catch (Exception ex)
            {
                WriteLogLine($"Exception in Generate ({sourceBuildInputs.TargetTypeInfo.Name})");
                WriteLogLine(ex.ToString());
                throw;
            }
        }

        private static bool IsXSymbolImpl(ITypeSymbol? typeSymbol, string ns1, string typeName)
        {
            Debug.Assert(!ns1.Contains("."));
            Debug.Assert(!typeName.Contains("."));

            if (typeSymbol is null) return false;

            if (typeSymbol.Name != typeName) return false;

            var containingNamespaceSymbol = typeSymbol.ContainingNamespace;

            if (containingNamespaceSymbol is null) return false;

            if (containingNamespaceSymbol.Name != ns1) return false;

            if (containingNamespaceSymbol.ContainingNamespace is null) return false;

            if (!containingNamespaceSymbol.ContainingNamespace.IsGlobalNamespace) return false;

            return true;
        }


        private static bool IsAssignableToIXImpl(ITypeSymbol? typeSymbol, Func<ITypeSymbol, bool> isXTypeFunc, Func<ITypeSymbol, bool> isAssignableToXFunc)
        {
            if (typeSymbol is null) return false;

            if (isXTypeFunc(typeSymbol)) return true;

            if (typeSymbol.AllInterfaces.Any((Func<INamedTypeSymbol, bool>)isXTypeFunc)) return true;

            // ジェネリック型の型パラメータの場合は型パラメータの制約を再帰的に確認
            if (typeSymbol is ITypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ConstraintTypes.Any(isAssignableToXFunc))
            {
                return true;
            }

            return false;
        }

        private static bool IsXAttributedMemberImpl(ISymbol? symbol, Func<INamedTypeSymbol,bool> isXAttributeSymbol)
        {
            if (symbol is null) return false;

            foreach (var attributeData in symbol.GetAttributes())
            {
                if (attributeData.AttributeClass is not null && isXAttributeSymbol(attributeData.AttributeClass))
                {
                    return true;
                }
            }

            return false;
        }


        internal static bool IsAutomaticDisposeImplAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, AutomaticDisposeImplAttributeName);

        internal static bool IsEnableAutomaticDisposeAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, EnableAutomaticDisposeAttributeName);

        internal static bool IsDisableAutomaticDisposeAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, DisableAutomaticDisposeAttributeName);

        internal static bool IsUnmanagedResourceReleaseMethodAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, UnmanagedResourceReleaseMethodAttributeName);

        internal static bool IsManagedObjectDisposeMethodAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, ManagedObjectDisposeMethodAttributeName);

        internal static bool IsManagedObjectAsyncDisposeMethodAttribute(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, AttributeDefinedNameSpace, ManagedObjectAsyncDisposeMethodAttributeName);

        internal static bool IsIDisposable(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, "System", "IDisposable");

        internal static bool IsIAsyncDisposable(ITypeSymbol? typeSymbol) => IsXSymbolImpl(typeSymbol, "System", "IAsyncDisposable");

        internal static bool IsAssignableToIDisposable(ITypeSymbol? typeSymbol) => IsAssignableToIXImpl(typeSymbol, IsIDisposable, IsAssignableToIDisposable);

        internal static bool IsAssignableToIAsyncDisposable(ITypeSymbol? typeSymbol) => IsAssignableToIXImpl(typeSymbol, IsIAsyncDisposable, IsAssignableToIAsyncDisposable);

        internal static bool IsEnableAutomaticDisposeAttributedMember(ISymbol symbol) => IsXAttributedMemberImpl(symbol, IsEnableAutomaticDisposeAttribute);

        internal static bool IsDisableAutomaticDisposeAttributedMember(ISymbol symbol) => IsXAttributedMemberImpl(symbol, IsDisableAutomaticDisposeAttribute);

        internal static bool IsUnmanagedResourceReleaseMethodAttributedMember(ISymbol symbol) => IsXAttributedMemberImpl(symbol, IsUnmanagedResourceReleaseMethodAttribute);

        internal static bool IsManagedObjectDisposeMethodAttributedMember(ISymbol symbol) => IsXAttributedMemberImpl(symbol, IsManagedObjectDisposeMethodAttribute);

        internal static bool IsManagedObjectAsyncDisposeMethodAttributedMember(ISymbol symbol) => IsXAttributedMemberImpl(symbol, IsManagedObjectAsyncDisposeMethodAttribute);

        internal static bool IsAutomaticDisposeImplSubClass(INamedTypeSymbol? namedTypeSymbol)
        {
            if (namedTypeSymbol is null)
            {
                return false;
            }

            var isAutomaticDisposeImplAnnotationed = namedTypeSymbol.GetAttributes().Select(attrData => attrData.AttributeClass).Any(IsAutomaticDisposeImplAttribute);

            if (isAutomaticDisposeImplAnnotationed)
            {
                return true;
            }

            if (namedTypeSymbol.BaseType is null || namedTypeSymbol.BaseType.SpecialType == SpecialType.System_Object)
            {
                return false;
            }

            return IsAutomaticDisposeImplSubClass(namedTypeSymbol.BaseType);
        }

        internal static bool TryGetBaseClassOwnedIsDisposableSymbol(INamedTypeSymbol? namedTypeSymbol, out ISymbol symbol)
        {
            if (namedTypeSymbol is null || namedTypeSymbol.SpecialType == SpecialType.System_Object)
            {
                symbol = null!;
                return false;
            }

            symbol = namedTypeSymbol.GetMembers().FirstOrDefault(member => member.Name == "IsDisposed" && !member.IsStatic)!;

            if (symbol is null)
            {
                return TryGetBaseClassOwnedIsDisposableSymbol(namedTypeSymbol.BaseType, out symbol);
            }
            else
            {
                return true;
            }
        }
    }
}
