namespace OptiGraphExtensions.Entities
{
    /// <summary>
    /// Represents the type of saved query for the Query Library.
    /// </summary>
    public enum QueryType
    {
        /// <summary>
        /// Visual query built using the query builder UI.
        /// </summary>
        Visual = 0,

        /// <summary>
        /// Raw GraphQL query entered directly by the user.
        /// </summary>
        Raw = 1
    }
}
