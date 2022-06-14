using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer3.Contrib.Nhibernate.Models
{
    internal class RefreshToken
    {
        /// <summary>
        /// Gets or sets the ClientId
        /// </summary>
        public string ClientId => AccessToken.ClientId;

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

        /// <summary>
        /// Gets the subject identifier.
        /// </summary>
        public string SubjectId => AccessToken.SubjectId;

        /// <summary>
        /// Gets the scopes
        /// </summary>
        public IEnumerable<string> Scopes => AccessToken.Scopes.Select(x => x.Name);
    }
}
