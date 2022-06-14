using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServer3.Contrib.Nhibernate.Models
{
    /// <summary>
    /// Models an Authorization Code as stored in the database Token store
    /// </summary>
    internal class AuthorizationCode
    {
        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public ClientLite Client { get; set; }

        /// <summary>
        /// Gets or sets the subject.
        /// </summary>
        public ClaimsPrincipalLite Subject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this code is an OpenID Connect code.
        /// </summary>
        public bool IsOpenId { get; set; }

        /// <summary>
        /// Gets or sets the requested scopes.
        /// </summary>
        public IEnumerable<ScopeLite> RequestedScopes { get; set; }

        /// <summary>
        /// Gets or sets the redirect URI.
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets the nonce.
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether consent was shown.
        /// </summary>
        public bool WasConsentShown { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the code challenge.
        /// </summary>
        public string CodeChallenge { get; set; }

        /// <summary>
        /// Gets or sets the code challenge method.
        /// </summary>
        public string CodeChallengeMethod { get; set; }

        /// <summary>
        /// Gets or sets the subject identifier.
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the scopes.
        /// </summary>
        public IEnumerable<string> Scopes => RequestedScopes.Select(x => x.Name);
    }
}
