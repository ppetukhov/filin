using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Filin.Generators.Extensions;
using Filin.Generators.Specs.Types;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Filin.Generators;

partial class CodeGenerator
{
    sealed class QueryEmitter(GenerationOptions options)
    {
        static readonly TypeSyntax ProjectionIdTypeSyntax = ParseTypeName(Constants.ProjectionIdTypeFullName);
        static readonly TypeSyntax ImmutableHashSetTagsTypeSyntax = ParseTypeName($"global::System.Collections.Immutable.ImmutableHashSet<{Constants.TagTypeFullName}>");
        static readonly TypeSyntax ImmutableArrayTagParameterTypeSyntax = ParseTypeName($"global::System.Collections.Immutable.ImmutableArray<({Constants.IdentityTypeFullName}, global::System.Collections.Immutable.ImmutableArray<int>)>");
        static readonly SyntaxToken IsMatchTagParamToken = Identifier("IsMatchTagParam");
        static readonly SyntaxToken CreateQueryCoreToken = Identifier("CreateQueryCore");
        static readonly SyntaxToken CreateProjectionIdCoreToken = Identifier("CreateProjectionIdCore");
        static readonly SyntaxToken QueryFieldToken = Identifier("Query");
        static readonly SyntaxToken QueryPartsToken = Identifier("QueryParts");
        static readonly SyntaxToken QueryTagParamsToken = Identifier("QueryTagParams");

        readonly MethodDeclarationSyntax GetQueryWithTagParamsMethod = CreateGetQueryMethod(Identifier(options.GetQueryMethod), hasTagParams: true);
        readonly MethodDeclarationSyntax GetQueryMethod = CreateGetQueryMethod(Identifier(options.GetQueryMethod), hasTagParams: false);

        readonly MethodDeclarationSyntax GetProjectionIdWithTagParamsMethod = CreateGetProjectionIdMethod(Identifier(options.ProjectorIdProperty), Identifier(options.GetProjectionIdMethod), hasTagParams: true);
        readonly MethodDeclarationSyntax GetProjectionIdMethod = CreateGetProjectionIdMethod(Identifier(options.ProjectorIdProperty), Identifier(options.GetProjectionIdMethod), hasTagParams: false);

        static readonly ClassDeclarationSyntax QueriesGeneratedSyntax = ClassDeclaration(
            attributeLists: SingletonList(Emitter.GeneratedCodeAttributeSyntax),
            modifiers: TokenList(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.StaticKeyword)),
            identifier: Emitter.QueriesGeneratedToken,
            typeParameterList: null,
            parameterList: null,
            baseList: null,
            constraintClauses: default,
            members: default);


        static readonly MethodDeclarationSyntax IsMatchTagParamMethodSyntax = IsMatchTagParamMethod();
        static readonly MethodDeclarationSyntax CreateQueryCoreMethodSyntax = CreateQueryCoreMethod();
        static readonly MethodDeclarationSyntax CreateProjectionIdCoreSyntax = CreateProjectionIdCore();

        // internal static readonly DcbQuery Query = new DcbQuery([new DcbQueryItem(["Created", "CourseBooked"], ["course:77"])]);
        static FieldDeclarationSyntax CreateQueryField(bool isPrivate, ImmutableArray<QueryItem> queryItems)
        {
            var expr = ObjectCreationExpression(Emitter.QueryTypeSyntax, ArgumentList().AddArguments(Argument(QueryItemsCollectionExpression(queryItems))), null);

            var modifiers = isPrivate
                ? TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                : TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

            return FieldDeclaration(
                    attributeLists: default,
                    modifiers: modifiers,
                    declaration: VariableDeclaration(
                        Emitter.QueryTypeSyntax,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    identifier: QueryFieldToken,
                                    argumentList: null,
                                    initializer: EqualsValueClause(expr)
                            )
                        )
                    )
                );
        }

        // internal? static readonly ImmutableArray<DcbQueryItem> QueryParts = [new DcbQueryItem(["Created", "CourseBooked"], ["course:77"])]);
        static FieldDeclarationSyntax CreateQueryItemsArrayField(bool isPrivate, ImmutableArray<QueryItem> queryItems)
        {
            var modifiers = isPrivate
                ? TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                : TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

            return FieldDeclaration(
                    attributeLists: default,
                    modifiers: modifiers,
                    declaration: VariableDeclaration(
                        Emitter.ImmutableArrayQueryItemTypeSyntax,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    identifier: QueryPartsToken,
                                    argumentList: null,
                                    initializer: EqualsValueClause(QueryItemsCollectionExpression(queryItems))
                            )
                        )
                    )
                );
        }

        // static readonly ImmutableArray<(Identity, ImmutableArray<int>)> QueryTagParams = [("course:", [0]), ("student:", [1])];
        static FieldDeclarationSyntax CreateQueryTagParamsField(bool isPrivate, ImmutableArray<TagParameter> tagParameters)
        {
            var modifiers = isPrivate
                ? TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))
                : TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

            return FieldDeclaration(
                    attributeLists: default,
                    modifiers: modifiers,
                    declaration: VariableDeclaration(
                        ImmutableArrayTagParameterTypeSyntax,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                    identifier: QueryTagParamsToken,
                                    argumentList: null,
                                    initializer: EqualsValueClause(TagParameterCollectionExpression(tagParameters))
                            )
                        )
                    )
                );
        }

        static CollectionExpressionSyntax TagParameterCollectionExpression(ImmutableArray<TagParameter> tagParameters)
        {
            var queryItemCreation = new List<CollectionElementSyntax>(capacity: tagParameters.Length);

            foreach (var (tagParam, idxs) in tagParameters)
            {
                var collectionElemetTagList = new List<CollectionElementSyntax>(capacity: idxs.Length);

                foreach (var idx in idxs)
                {
                    collectionElemetTagList.Add(ExpressionElement(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(idx))));
                }

                var tags = CollectionExpression(SeparatedList(collectionElemetTagList));

                var evts = TupleExpression(SeparatedList([Argument((tagParam + Constants.TagSuffixSeparator).GetLiteralExpression()), Argument(tags)]));

                queryItemCreation.Add(ExpressionElement(evts));
            }

            return CollectionExpression(SeparatedList(queryItemCreation));
        }

        // [new DcbQueryItem(["Created", "CourseBooked"], ["course:77"])]
        static CollectionExpressionSyntax QueryItemsCollectionExpression(ImmutableArray<QueryItem> queryItems)
        {
            var queryItemCreation = new List<ObjectCreationExpressionSyntax>(capacity: queryItems.Length);

            foreach (var queryItem in queryItems)
            {
                var collectionElemetEventTypeList = new List<CollectionElementSyntax>(capacity: queryItem.EventTypes.Length);

                foreach (var evt in queryItem.EventTypes)
                {
                    collectionElemetEventTypeList.Add(ExpressionElement(evt.GetLiteralExpression()));
                }

                var evts = CollectionExpression(SeparatedList(collectionElemetEventTypeList));

                var collectionElemetTagList = new List<CollectionElementSyntax>(capacity: queryItem.Tags.Length);

                foreach (var tag in queryItem.Tags)
                {
                    collectionElemetTagList.Add(ExpressionElement(tag.GetLiteralExpression()));
                }

                var tags = CollectionExpression(SeparatedList(collectionElemetTagList));

                queryItemCreation.Add(ObjectCreationExpression(Emitter.QueryItemTypeSyntax, ArgumentList().AddArguments(Argument(evts), Argument(tags)), null));
            }

            return CollectionExpression(SeparatedList(queryItemCreation.Select(ExpressionElement).Cast<CollectionElementSyntax>()));
        }

        // static bool IsMatchTagParam(Identity tagParam, Tag tag) => tag.AsSpan().StartsWith(tagParam.AsSpan())
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax IsMatchTagParamMethod() =>
            MethodDeclaration(
                attributeLists: SingletonList(Emitter.AggressiveInliningAttributeSyntax),
                modifiers: TokenList(Token(SyntaxKind.StaticKeyword)),
                returnType: ParseTypeName("bool"),
                explicitInterfaceSpecifier: null,
                identifier: IsMatchTagParamToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SeparatedList([
                        Parameter(attributeLists: default, modifiers: default, ParseTypeName(Constants.IdentityTypeFullName), Identifier("tagParam"), null),
                        Parameter(attributeLists: default, modifiers: default, ParseTypeName(Constants.TagTypeFullName), Identifier("tag"), null)]
                    )),
                body: null,
                expressionBody: ArrowExpressionClause(InvocationExpression(
                        InvocationExpression(IdentifierName(Identifier("tag")).Member("AsSpan")).Member("StartsWith"),
                            ArgumentList().AddArguments(Argument(
                                InvocationExpression(IdentifierName(Identifier("tagParam")).Member("AsSpan")))
                                ))),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        // static DcbQuery CreateQueryCore(ImmutableArray<DcbQueryItem> queryParts, ImmutableArray<(Identity, ImmutableArray<int>)> queryTagParams, ImmutableHashSet<Tag> tags)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax CreateQueryCoreMethod()
        {
            #region Method Block
            var statements = Block(
                // var queryItems = queryParts.ToList();
                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                    .AddDeclarationVariables(
                        VariableDeclarator(Identifier("queryItems"), null,
                            EqualsValueClause(InvocationExpression(IdentifierName(Identifier("queryParts")).Member("ToList"))))),

                // foreach (var tagParam in queryTagParams)
                ForEachStatement(IdentifierName("var"), Identifier("tagParam"), IdentifierName(Identifier("queryTagParams")),
                    Block(
                        // var tagFound = false;
                        LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                            .AddDeclarationVariables(
                                VariableDeclarator(Identifier("tagFound"), null,
                                    EqualsValueClause(IdentifierName("false")))),

                        // foreach (var tag in tags)
                        ForEachStatement(IdentifierName("var"), Identifier("tag"), IdentifierName(Identifier("tags")),
                            Block(
                                // if (IsMatchTagParam(tagParam.Item1, tag))
                                IfStatement(
                                        InvocationExpression(IdentifierName(IsMatchTagParamToken),
                                            ArgumentList()
                                                .AddArguments(
                                                    Argument(IdentifierName(Identifier("tagParam")).Member("Item1")),
                                                    Argument(IdentifierName(Identifier("tag")))
                                                )
                                        ),
                                        Block(
                                            // foreach (var queryIdx in tagParam.Item2)
                                            ForEachStatement(IdentifierName("var"), Identifier("queryIdx"), IdentifierName(Identifier("tagParam")).Member("Item2"),
                                                Block(
                                                    // var queryItem = queryItems[queryIdx];
                                                    LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                                                        .AddDeclarationVariables(
                                                            VariableDeclarator(Identifier("queryItem"), null,
                                                                EqualsValueClause(
                                                                    ElementAccessExpression(
                                                                        IdentifierName(Identifier("queryItems")),
                                                                        BracketedArgumentList([Argument(IdentifierName(Identifier("queryIdx")))])
                                                                    )
                                                                )
                                                            )
                                                        ),

                                                    // queryItems[queryIdx] = queryItem with { Tags = queryItem.Tags.Add(tag) };
                                                    ExpressionStatement(
                                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                            ElementAccessExpression(
                                                                IdentifierName(Identifier("queryItems")),
                                                                BracketedArgumentList([Argument(IdentifierName(Identifier("queryIdx")))])
                                                            ),
                                                            WithExpression(
                                                                IdentifierName(Identifier("queryItem")),
                                                                InitializerExpression(SyntaxKind.WithInitializerExpression,
                                                                    SingletonSeparatedList<ExpressionSyntax>(
                                                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                                                IdentifierName(Identifier("Tags")),
                                                                            InvocationExpression(
                                                                                IdentifierName(Identifier("queryItem")).Member("Tags").Member("Add"),
                                                                                ArgumentList()
                                                                                    .AddArguments(
                                                                                        Argument(IdentifierName(Identifier("tag")))
                                                                                    )
                                                                            )
                                                                        )
                                                                    )
                                                                )
                                                            )
                                                        )
                                                    )
                                                )
                                            ),
                                            // tagFound = true;
                                            ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("tagFound"), IdentifierName("true")))
                                        )
                                )
                            )
                        ),
                        // if (!tagFound)
                        IfStatement(
                            PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IdentifierName("tagFound")),
                            Block(
                                // var ex = new ArgumentException($"Required tag started with {tagParam.Item1}", nameof(tags));
                                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                                    .AddDeclarationVariables(
                                        VariableDeclarator(Identifier("ex"), null,
                                            EqualsValueClause(ObjectCreationExpression(ParseTypeName("global::System.ArgumentException"),
                                                ArgumentList()
                                                    .AddArguments(
                                                        Argument(
                                                            InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
                                                                .AddContents(
                                                                    InterpolatedStringText().WithTextToken(Token(
                                                                        TriviaList(),
                                                                        SyntaxKind.InterpolatedStringTextToken,
                                                                        "Required tag started with ",
                                                                        string.Empty,
                                                                        TriviaList())),
                                                                    Interpolation(IdentifierName(Identifier("tagParam")).Member("Item1"))
                                                                )
                                                        ),
                                                        Argument(
                                                            InvocationExpression(IdentifierName(Identifier("nameof")),
                                                                ArgumentList()
                                                                    .AddArguments(
                                                                        Argument(IdentifierName(Identifier("tags")))
                                                                    )
                                                            )
                                                        )
                                                    ), null)
                                            )
                                        )
                                    ),
                                // ex.Data["TagParameter"] = tagParam.Item1.ToString();
                                ExpressionStatement(
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        ElementAccessExpression(
                                            IdentifierName(Identifier("ex")).Member("Data"),
                                            BracketedArgumentList([Argument("TagParameter".GetLiteralExpression())])
                                        ),
                                        InvocationExpression(
                                            IdentifierName(Identifier("tagParam")).Member("Item1").Member("ToString")
                                        )
                                    )
                                ),
                                // ex.Data["Tags"] = string.Join(';', tags);
                                ExpressionStatement(
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        ElementAccessExpression(
                                            IdentifierName(Identifier("ex")).Member("Data"),
                                            BracketedArgumentList([Argument("Tags".GetLiteralExpression())])
                                        ),
                                        InvocationExpression(
                                            IdentifierName(Identifier("string")).Member("Join"),
                                            ArgumentList()
                                                .AddArguments(
                                                    Argument(";".GetLiteralExpression()),
                                                    Argument(IdentifierName(Identifier("tags")))
                                                )
                                        )
                                    )
                                ),
                                // throw ex;
                                ThrowStatement(IdentifierName(Identifier("ex")))
                            )
                        )
                    )
                ),

                // return new DcbQuery([.. queryItems]);
                ReturnStatement(ObjectCreationExpression(Emitter.QueryTypeSyntax, ArgumentList()
                    .AddArguments(
                        Argument(CollectionExpression([SpreadElement(Token(SyntaxKind.DotDotToken), IdentifierName(Identifier("queryItems")))]))
                    ), null)
                )
            );
            #endregion

            return MethodDeclaration(
                attributeLists: default,
                modifiers: TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.QueryTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: CreateQueryCoreToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SeparatedList([
                        Parameter(attributeLists: default, modifiers: default, Emitter.ImmutableArrayQueryItemTypeSyntax, Identifier("queryParts"), null),
                        Parameter(attributeLists: default, modifiers: default, ImmutableArrayTagParameterTypeSyntax, Identifier("queryTagParams"), null),
                        Parameter(attributeLists: default, modifiers: default, ImmutableHashSetTagsTypeSyntax, Identifier("tags"), null)]
                    )),
                body: statements,
                expressionBody: null,
                semicolonToken: Token(SyntaxKind.None));
        }

        // static DcbQuery CreateProjectionIdCore(ImmutableArray<(Identity, ImmutableArray<int>)> queryTagParams, ImmutableHashSet<Tag> tags)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax CreateProjectionIdCore()
        {
            #region Method Block
            var statements = Block(
                // var minimumLength = queryTagParams.Length;
                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                    .AddDeclarationVariables(
                        VariableDeclarator(Identifier("minimumLength"), null,
                            EqualsValueClause(IdentifierName(Identifier("queryTagParams")).Member("Length")))),

                // foreach (var tag in tags)
                ForEachStatement(IdentifierName("var"), Identifier("tag"), IdentifierName(Identifier("tags")),
                    Block(
                        // minimumLength += tag.AsSpan().Length;
                        ExpressionStatement(AssignmentExpression(SyntaxKind.AddAssignmentExpression, IdentifierName(Identifier("minimumLength")), InvocationExpression(IdentifierName(Identifier("tag")).Member("AsSpan")).Member("Length")))
                    )
                ),

                // var buf = global::System.Buffers.ArrayPool<byte>.Shared.Rent(minimumLength);
                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                    .AddDeclarationVariables(
                        VariableDeclarator(Identifier("buf"), null,
                            EqualsValueClause(InvocationExpression(ParseTypeName("global::System.Buffers.ArrayPool<byte>").Member("Shared").Member("Rent"),
                                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(Identifier("minimumLength"))))))))),

                // var writtenBytes = 0;
                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                    .AddDeclarationVariables(
                        VariableDeclarator(Identifier("writtenBytes"), null,
                            EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))),

                // try {} finaly{}
                TryStatement(
                    block: Block(
                        // var span = new Span<byte>(buf);
                        LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                            .AddDeclarationVariables(
                                VariableDeclarator(Identifier("span"), null,
                                    EqualsValueClause(ObjectCreationExpression(ParseTypeName("global::System.Span<byte>"),
                                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName(Identifier("buf"))))), null)))),

                        // foreach (var (tagParam, _) in queryTagParams)
                        ForEachVariableStatement(
                            DeclarationExpression(IdentifierName("var"),
                                ParenthesizedVariableDesignation()
                                    .AddVariables(
                                        SingleVariableDesignation("tagParam".ToIdentifier()),
                                        DiscardDesignation()
                                    )
                                ),
                            IdentifierName(Identifier("queryTagParams")),
                            Block(
                                // var tagFound = false;
                                LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                                    .AddDeclarationVariables(
                                        VariableDeclarator(Identifier("tagFound"), null,
                                            EqualsValueClause(IdentifierName("false")))),

                                // foreach (var tag in tags)
                                ForEachStatement(IdentifierName("var"), Identifier("tag"), IdentifierName(Identifier("tags")),
                                    Block(
                                        // if (IsMatchTagParam(tagParam.Item1, tag))
                                        IfStatement(
                                                InvocationExpression(IdentifierName(IsMatchTagParamToken),
                                                    ArgumentList()
                                                        .AddArguments(
                                                            Argument(IdentifierName(Identifier("tagParam"))),
                                                            Argument(IdentifierName(Identifier("tag")))
                                                        )
                                                ),
                                                Block(
                                                    // var tagSpan = foundTag.AsSpan();
                                                    LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                                                        .AddDeclarationVariables(
                                                            VariableDeclarator(Identifier("tagSpan"), null,
                                                                EqualsValueClause(InvocationExpression(IdentifierName("tag").Member("AsSpan"))))),

                                                    // tagSpan.CopyTo(span);
                                                    ExpressionStatement(InvocationExpression(IdentifierName(Identifier("tagSpan")).Member("CopyTo"),
                                                                            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(Identifier("span"))))))),

                                                    // span[tagSpan.Length] = (byte)'&';
                                                    ExpressionStatement(
                                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                            ElementAccessExpression(
                                                                IdentifierName(Identifier("span")),
                                                                BracketedArgumentList([Argument(IdentifierName(Identifier("tagSpan")).Member("Length"))])
                                                            ),
                                                            CastExpression(ParseTypeName("byte"), LiteralExpression(SyntaxKind.CharacterLiteralExpression, Literal('&')))
                                                        )
                                                    ),

                                                    // span = span[(tagSpan.Length + 1)..];
                                                    ExpressionStatement(
                                                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                            IdentifierName(Identifier("span")),
                                                            ElementAccessExpression(
                                                                IdentifierName(Identifier("span")),
                                                                BracketedArgumentList([Argument(
                                                                    RangeExpression(ParenthesizedExpression(
                                                                        BinaryExpression(SyntaxKind.AddExpression,
                                                                            IdentifierName(Identifier("tagSpan")).Member("Length"),
                                                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))),
                                                                        Token(SyntaxKind.DotDotToken),
                                                                        null))
                                                                ])
                                                            )
                                                        )
                                                    ),

                                                    // writtenBytes += tagSpan.Length + 1;
                                                    ExpressionStatement(
                                                        AssignmentExpression(SyntaxKind.AddAssignmentExpression,
                                                            IdentifierName(Identifier("writtenBytes")),
                                                            BinaryExpression(SyntaxKind.AddExpression,
                                                                IdentifierName(Identifier("tagSpan")).Member("Length"),
                                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))
                                                        )
                                                    ),
                                                    // tagFound = true;
                                                    ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName("tagFound"), IdentifierName("true")))
                                                )
                                        )
                                    )
                                ),
                                // if (!tagFound)
                                IfStatement(
                                    PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, IdentifierName("tagFound")),
                                    Block(
                                        // var ex = new ArgumentException($"Required tag started with {tagParam.Item1}", nameof(tags));
                                        LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                                            .AddDeclarationVariables(
                                                VariableDeclarator(Identifier("ex"), null,
                                                    EqualsValueClause(ObjectCreationExpression(ParseTypeName("global::System.ArgumentException"),
                                                        ArgumentList()
                                                            .AddArguments(
                                                                Argument(
                                                                    InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
                                                                        .AddContents(
                                                                            InterpolatedStringText().WithTextToken(Token(
                                                                                TriviaList(),
                                                                                SyntaxKind.InterpolatedStringTextToken,
                                                                                "Required tag started with ",
                                                                                string.Empty,
                                                                                TriviaList())),
                                                                            Interpolation(IdentifierName(Identifier("tagParam")))
                                                                        )
                                                                ),
                                                                Argument(
                                                                    InvocationExpression(IdentifierName(Identifier("nameof")),
                                                                        ArgumentList()
                                                                            .AddArguments(
                                                                                Argument(IdentifierName(Identifier("tags")))
                                                                            )
                                                                    )
                                                                )
                                                            ), null)
                                                    )
                                                )
                                            ),
                                        // ex.Data["TagParameter"] = tagParam.Item1.ToString();
                                        ExpressionStatement(
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                ElementAccessExpression(
                                                    IdentifierName(Identifier("ex")).Member("Data"),
                                                    BracketedArgumentList([Argument("TagParameter".GetLiteralExpression())])
                                                ),
                                                InvocationExpression(
                                                    IdentifierName(Identifier("tagParam")).Member("ToString")
                                                )
                                            )
                                        ),
                                        // ex.Data["Tags"] = string.Join(';', tags);
                                        ExpressionStatement(
                                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                                ElementAccessExpression(
                                                    IdentifierName(Identifier("ex")).Member("Data"),
                                                    BracketedArgumentList([Argument("Tags".GetLiteralExpression())])
                                                ),
                                                InvocationExpression(
                                                    IdentifierName(Identifier("string")).Member("Join"),
                                                    ArgumentList()
                                                        .AddArguments(
                                                            Argument(";".GetLiteralExpression()),
                                                            Argument(IdentifierName(Identifier("tags")))
                                                        )
                                                )
                                            )
                                        ),
                                        // throw ex;
                                        ThrowStatement(IdentifierName(Identifier("ex")))
                                    )
                                )
                            )
                        ),

                        // writtenBytes -= 1;
                        ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SubtractAssignmentExpression,
                                IdentifierName(Identifier("writtenBytes")),
                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1))
                            )
                        ),

                        // var raw = new byte[writtenBytes];
                        LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")))
                            .AddDeclarationVariables(
                                VariableDeclarator(Identifier("raw"), null,
                                    EqualsValueClause(ArrayCreationExpression(ArrayType(ParseTypeName("byte"),
                                            SingletonList(
                                                ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(IdentifierName(Identifier("writtenBytes"))))
                                            )
                                        ))
                                    )
                                )
                            ),

                        //Array.Copy(buf, raw, writtenBytes);
                        ExpressionStatement(InvocationExpression(ParseTypeName("global::System.Array").Member("Copy"),
                            ArgumentList().AddArguments(
                                Argument(IdentifierName(Identifier("buf"))),
                                Argument(IdentifierName(Identifier("raw"))),
                                Argument(IdentifierName(Identifier("writtenBytes")))
                            ))),

                        // return new ProjectionId(projectorId, new Identity(raw));
                        ReturnStatement(ObjectCreationExpression(ProjectionIdTypeSyntax, ArgumentList()
                            .AddArguments(
                                Argument(IdentifierName(Identifier("projectorId"))),
                                Argument(ObjectCreationExpression(ParseTypeName(Constants.IdentityTypeFullName),
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(Identifier("raw"))))), null))
                            ), null)
                        )
                    ),
                    catches: default,
                    @finally: FinallyClause(
                        Block(
                            ExpressionStatement(
                                InvocationExpression(ParseTypeName("global::System.Buffers.ArrayPool<byte>").Member("Shared").Member("Return"),
                                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName(Identifier("buf"))))))
                                )
                        )
                    ))
            );
            #endregion

            return MethodDeclaration(
                attributeLists: default,
                modifiers: TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)),
                returnType: ProjectionIdTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: CreateProjectionIdCoreToken,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(
                    SeparatedList([
                        Parameter(attributeLists: default, modifiers: default, Emitter.ProjectoIdTypeSyntax, Identifier("projectorId"), null),
                        Parameter(attributeLists: default, modifiers: default, ImmutableArrayTagParameterTypeSyntax, Identifier("queryTagParams"), null),
                        Parameter(attributeLists: default, modifiers: default, ImmutableHashSetTagsTypeSyntax, Identifier("tags"), null)]
                    )),
                body: statements,
                expressionBody: null,
                semicolonToken: Token(SyntaxKind.None));
        }

        // static DcbQuery methodName(ImmutableHashSet<Tag> tags)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax CreateGetQueryMethod(SyntaxToken methodName, bool hasTagParams) =>
            MethodDeclaration(
                attributeLists: default,
                modifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                returnType: Emitter.QueryTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: methodName,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(SingletonSeparatedList(Parameter(attributeLists: default, modifiers: default, ImmutableHashSetTagsTypeSyntax, hasTagParams ? Identifier("tags") : Emitter.DiscardIdentifierToken, null))),
                body: null,
                expressionBody: hasTagParams
                    ? ArrowExpressionClause(
                        InvocationExpression(QualifiedName(IdentifierName(Emitter.QueriesGeneratedToken), IdentifierName(CreateQueryCoreToken)),
                            ArgumentList().AddArguments(
                                Argument(IdentifierName(QueryPartsToken)),
                                Argument(IdentifierName(QueryTagParamsToken)),
                                Argument(IdentifierName(Identifier("tags")))
                                )))
                    : ArrowExpressionClause(IdentifierName(QueryFieldToken)),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        // static ProjectionId methodName(ImmutableHashSet<Tag> tags)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static MethodDeclarationSyntax CreateGetProjectionIdMethod(SyntaxToken ProjectorIdIdentifier, SyntaxToken methodName, bool hasTagParams) =>
            MethodDeclaration(
                attributeLists: default,
                modifiers: TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)),
                returnType: ProjectionIdTypeSyntax,
                explicitInterfaceSpecifier: null,
                identifier: methodName,
                typeParameterList: default,
                constraintClauses: default,
                parameterList: ParameterList(SingletonSeparatedList(Parameter(attributeLists: default, modifiers: default, ImmutableHashSetTagsTypeSyntax, hasTagParams ? Identifier("tags") : Emitter.DiscardIdentifierToken, null))),
                body: null,
                expressionBody: hasTagParams
                    ? ArrowExpressionClause(
                        InvocationExpression(QualifiedName(IdentifierName(Emitter.QueriesGeneratedToken), IdentifierName(CreateProjectionIdCoreToken)),
                            ArgumentList().AddArguments(
                                Argument(IdentifierName(ProjectorIdIdentifier)),
                                Argument(IdentifierName(QueryTagParamsToken)),
                                Argument(IdentifierName(Identifier("tags")))
                                )))
                    : ArrowExpressionClause(ObjectCreationExpression(ProjectionIdTypeSyntax, ArgumentList()
                            .AddArguments(
                                Argument(IdentifierName(ProjectorIdIdentifier)),
                                Argument(DefaultExpression(ParseTypeName(Constants.IdentityTypeFullName)))
                            ), null)),
                semicolonToken: Token(SyntaxKind.SemicolonToken));

        internal void EmitQueries(SourceProductionContext ctx, ImmutableArray<Query> queries)
        {
            if (queries is { IsDefaultOrEmpty: true })
            {
                return;
            }

            var cu = CompilationUnit()
                .AddUsings(UsingDirective(ParseName("global::System.Collections.Immutable")))
                .AddMembers(QueriesGeneratedSyntax.AddMembers(IsMatchTagParamMethodSyntax, CreateQueryCoreMethodSyntax, CreateProjectionIdCoreSyntax))
                .AddMembers([.. GenerateClassHeirerarhy(ctx, queries)])
                .NormalizeWhitespace();

            ctx.CancellationToken.ThrowIfCancellationRequested();

            var sourceText = SourceText.From(cu.ToFullString(), Encoding.UTF8);

            ctx.AddSource("Queries.g.cs", sourceText);
        }

        IEnumerable<NamespaceDeclarationSyntax> GenerateClassHeirerarhy(SourceProductionContext ctx, ImmutableArray<Query> queries)
        {
            IEnumerable<MemberDeclarationSyntax> MembersFactory((ObjectSpec TargetType, Query Item) tuple)
            {
                var (_, query) = tuple;

                var tagParameters = query is QueryParametrized qp ? qp.TagParameters : [];

                return tagParameters.IsDefaultOrEmpty
                    ? [CreateQueryField(isPrivate: true, queryItems: query.Items), GetQueryMethod, GetProjectionIdMethod]
                    : [
                        CreateQueryItemsArrayField(isPrivate: true, queryItems: query.Items),
                        CreateQueryTagParamsField(isPrivate: true, tagParameters: tagParameters),
                        GetQueryWithTagParamsMethod,
                        GetProjectionIdWithTagParamsMethod
                    ];
            }

            return Emitter.GenerateClassHeirerarhyCore(queries.Select(static x => (x.TargetType, x)), MembersFactory, ctx.CancellationToken);
        }
    }
}
