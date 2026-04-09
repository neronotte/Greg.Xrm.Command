using System;

namespace Greg.Xrm.Command
{
    /// <summary>
    /// Marks a class as a Dataverse plugin and defines the plugin step registration metadata.
    /// When applied to a plugin class, this attribute is scanned by <c>pacx plugin register-attributes</c>
    /// to automatically register plugin steps in Dataverse.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CrmPluginStepAttribute : Attribute
    {
        /// <summary>
        /// The message name (e.g., "Create", "Update", "Delete", "Retrieve", "Assign").
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The entity logical name (e.g., "account", "contact"). Use "*" for all entities.
        /// </summary>
        public string Entity { get; } = "*";

        /// <summary>
        /// The execution stage: PreValidation (10), PreOperation (20), PostOperation (40).
        /// Default is 40 (PostOperation).
        /// </summary>
        public int Stage { get; set; } = 40;

        /// <summary>
        /// The execution mode: Synchronous (0) or Asynchronous (1).
        /// Default is 0 (Synchronous).
        /// </summary>
        public int ExecutionMode { get; set; } = 0;

        /// <summary>
        /// The deployment mode: Server (0), Offline (1), or Both (2).
        /// Default is 0 (Server).
        /// </summary>
        public int Deployment { get; set; } = 0;

        /// <summary>
        /// The execution order for multiple steps on the same message/entity.
        /// Default is 1.
        /// </summary>
        public int Rank { get; set; } = 1;

        /// <summary>
        /// The secure configuration for the step (encrypted, admin-only).
        /// </summary>
        public string? SecureConfiguration { get; set; }

        /// <summary>
        /// The unsecure configuration for the step (plain text).
        /// </summary>
        public string? UnsecureConfiguration { get; set; }

        /// <summary>
        /// Filter attributes — only fire the step when these attributes are modified.
        /// Only applicable for Update message.
        /// </summary>
        public string[]? FilteringAttributes { get; set; }

        /// <summary>
        /// Name of the step. Auto-generated if not specified.
        /// </summary>
        public string? Name { get; set; }

        public CrmPluginStepAttribute(string message, string entity = "*")
        {
            Message = message;
            Entity = entity;
        }
    }

    /// <summary>
    /// Defines an image to be registered for a plugin step.
    /// Apply this attribute to a property on the plugin class that returns the image data,
    /// or use it alongside <see cref="CrmPluginStepAttribute"/> to define images for that step.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CrmPluginImageAttribute : Attribute
    {
        /// <summary>
        /// The image name (friendly identifier).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The entity alias used in the plugin context to access this image.
        /// </summary>
        public string EntityAlias { get; }

        /// <summary>
        /// The image type: PreImage (0), PostImage (1), or Both (2).
        /// Default is 0 (PreImage).
        /// </summary>
        public int ImageType { get; set; } = 0;

        /// <summary>
        /// Comma-separated list of attributes to include in the image.
        /// Empty string means all attributes.
        /// </summary>
        public string? Attributes { get; set; }

        /// <summary>
        /// The message name this image belongs to. Must match a <see cref="CrmPluginStepAttribute"/>.
        /// </summary>
        public string? Message { get; set; }

        public CrmPluginImageAttribute(string name, string entityAlias)
        {
            Name = name;
            EntityAlias = entityAlias;
        }
    }

    /// <summary>
    /// Defines a webhook registration for a plugin step.
    /// When applied to a plugin class with <see cref="CrmPluginStepAttribute"/>,
    /// registers a webhook instead of a plugin assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CrmWebhookAttribute : Attribute
    {
        /// <summary>
        /// The webhook endpoint URL.
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// The HTTP method: POST (0) or GET (1). Default is 0 (POST).
        /// </summary>
        public int Method { get; set; } = 0;

        /// <summary>
        /// Authentication type: None (0), HeaderKey (1), SAS (2), OAuth (3).
        /// Default is 0 (None).
        /// </summary>
        public int AuthType { get; set; } = 0;

        /// <summary>
        /// The value for the authentication header (when AuthType is HeaderKey).
        /// </summary>
        public string? AuthHeaderValue { get; set; }

        /// <summary>
        /// The timeout in seconds for the webhook call. Default is 30.
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// Include the entity image data in the webhook payload.
        /// </summary>
        public bool IncludeEntityImage { get; set; }

        public CrmWebhookAttribute(string url)
        {
            Url = url;
        }
    }
}
