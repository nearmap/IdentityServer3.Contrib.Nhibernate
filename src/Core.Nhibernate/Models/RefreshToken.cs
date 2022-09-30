using System;

namespace IdentityServer3.Contrib.Nhibernate.Models
{
    internal class RefreshToken
    {
        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the life time.
        /// </summary>
        public int LifeTime { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Token AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the original subject that requiested the token.
        /// </summary>
        public ClaimsPrincipalLite Subject { get; set; }

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public int Version { get; set; }
    }
}
