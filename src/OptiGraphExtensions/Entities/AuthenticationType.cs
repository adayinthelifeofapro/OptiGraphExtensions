namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Authentication types supported for external API imports.
    /// </summary>
    public enum AuthenticationType
    {
        /// <summary>
        /// No authentication required.
        /// </summary>
        None = 0,

        /// <summary>
        /// API key passed in a custom header.
        /// </summary>
        ApiKey = 1,

        /// <summary>
        /// Basic authentication (username/password).
        /// </summary>
        Basic = 2,

        /// <summary>
        /// Bearer token authentication.
        /// </summary>
        Bearer = 3
    }
}
