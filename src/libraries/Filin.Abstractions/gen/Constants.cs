namespace Filin.Generators;

static class Constants
{
    /// <summary>
    /// Value is <c>Filin.ApplicationPartAttribute</c>.
    /// </summary>
    public const string ApplicationPartAttribute = "Filin.ApplicationPartAttribute";

    /// <summary>
    /// Value is <c>Filin.DeciderAttribute</c>.
    /// </summary>
    public const string DeciderAttribute = "Filin.DeciderAttribute";

    /// <summary>
    /// Defualt value is <c>Filin</c>.
    /// </summary>
    public const string DefaultEmittingNamespace = "Filin";

    /// <summary>
    /// Default value is <c>EventTypeExtensions</c>.
    /// </summary>
    public const string DefaultEventTypeExtensionsClassName = "EventTypeExtensions";

    /// <summary>
    /// Default value is <c>GetEventType</c>.
    /// </summary>
    public const string DefaultGetEventTypeMethod = "GetEventType";

    /// <summary>
    /// Default value is <c>GetQuery</c>.
    /// </summary>
    public const string DefaultQueryMethod = "GetQuery";

    /// <summary>
    /// Default value is <c>QueryExtensions</c>.
    /// </summary>
    public const string DefaultQueryExtensionsClass = "QueryExtensions";

    /// <summary>
    /// Default value is <c>GetProjectionId</c>.
    /// </summary>
    public const string DefaultGetProjectionIdMethod = "GetProjectionId";

    /// <summary>
    /// Default value is <c>GetTag</c>.
    /// </summary>
    public const string DefaultGetTagMethod = "GetTag";

    /// <summary>
    /// Default value is <c>GetTags</c>.
    /// </summary>
    public const string DefaultGetTagsMethod = "GetTags";

    /// <summary>
    /// Default value is <c>ProjectorId</c>.
    /// </summary>
    public const string DefaultProjectorIdProperty = "ProjectorId";

    /// <summary>
    /// Default value is <c>TagsGeneratedExtensions</c>.
    /// </summary>
    public const string DefaultTagExtensionsClassName = "TagExtensions";

    /// <summary>
    /// Value is <c>Filin.EventTagAttribute</c>.
    /// </summary>
    public const string EventTagAttribute = "Filin.EventTagAttribute";

    /// <summary>
    /// Value is <c>Filin.EventTypeAttribute</c>.
    /// </summary>
    public const string EventTypeAttribute = "Filin.EventTypeAttribute";

    /// <summary>
    /// Value is <c>global::Filin.EventType</c>.
    /// </summary>
    public const string EventTypeTypeFullName = "global::Filin.EventType";

    /// <summary>
    /// Value is <c>@</c>.
    /// </summary>
    public const char EventTypeVersionSeparator = '@';

    /// <summary>
    /// Value is <c>@</c>.
    /// </summary>
    public const char VersionSeparator = '@';

    /// <summary>
    /// Value is <c>global::Filin.IDecider`2</c>.
    /// </summary>
    public const string IDecider = "global::Filin.IDecider`2";

    /// <summary>
    /// Value is <c>global::Filin.IdSpan</c>.
    /// </summary>
    public const string IdSpanTypeFullName = "global::Filin.IdSpan";

    /// <summary>
    /// Value is <c>global::Filin.IWithQueryTags</c>.
    /// </summary>
    public const string IWithQueryTags = "global::Filin.IWithQueryTags";

    /// <summary>
    /// Value is <c>global::Filin.IProjector`1</c>.
    /// </summary>
    public const string IProjector = "global::Filin.IProjector`1";

    /// <summary>
    /// Value is <c>Filin.QueryEventTypesAttribute</c>.
    /// </summary>
    public const string QueryEventTypesAttribute = "Filin.QueryEventTypesAttribute";

    /// <summary>
    /// Value is <c>global::Filin.DcbQueryItem</c>.
    /// </summary>
    public const string QueryItemTypeFullName = "global::Filin.DcbQueryItem";

    /// <summary>
    /// Value is <c>Filin.QueryTagsAttribute</c>.
    /// </summary>
    public const string QueryTagsAttribute = "Filin.QueryTagsAttribute";

    /// <summary>
    /// Value is <c>Filin.QueryTagParametersAttribute</c>.
    /// </summary>
    public const string QueryTagParametersAttribute = "Filin.QueryTagParametersAttribute";

    /// <summary>
    /// Value is <c>global::Filin.DcbQuery</c>.
    /// </summary>
    public const string QueryTypeFullName = "global::Filin.DcbQuery";

    /// <summary>
    /// Value is <c>Filin.ProjectorIdAttribute</c>.
    /// </summary>
    public const string ProjectorIdAttribute = "Filin.ProjectorIdAttribute";

    /// <summary>
    /// Value is <c>global::Filin.ProjectorId</c>.
    /// </summary>
    public const string ProjectorIdTypeFullName = "global::Filin.ProjectorId";

    /// <summary>
    /// Value is <c>global::Filin.ProjectionId</c>.
    /// </summary>
    public const string ProjectionIdTypeFullName = "global::Filin.ProjectionId";

    /// <summary>
    /// Value is <c>:</c>.
    /// </summary>
    public const char TagSuffixSeparator = ':';

    /// <summary>
    /// Value is <c>global::Filin.Tag</c>.
    /// </summary>
    public const string TagTypeFullName = "global::Filin.Tag";

    /// <summary>
    /// Value is <c>global::Filin.Configuration.TypeManifestOptions</c>.
    /// </summary>
    public const string TypeManifestOptionsFullName = "global::Filin.Configuration.TypeManifestOptions";

    /// <summary>
    /// Value is <c>global::Filin.Hosting.TypeManifestProviderBase</c>.
    /// </summary>
    public const string TypeManifestProviderBaseFullName = "global::Filin.Hosting.TypeManifestProviderBase";

    /// <summary>
    /// Value is <c>Filin.TypeManifestProviderAttribute</c>.
    /// </summary>
    public const string TypeManifestProviderAttribute = "Filin.TypeManifestProviderAttribute";

    /// <summary>
    /// Value is <c>global::System.Collections.Immutable.ImmutableSortedSet&lt;global::Filin.Tag&gt;</c>.
    /// </summary>
    public const string ReturnTagTypeCollection = $"global::System.Collections.Immutable.ImmutableSortedSet<{TagTypeFullName}>";
}
