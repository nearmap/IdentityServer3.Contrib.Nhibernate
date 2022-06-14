using System;
using System.Linq;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;

namespace Core.Nhibernate.IntegrationTests.Serialization
{
    internal class ScopeLite
    {
        public string Name { get; set; }
    }

    internal class ScopeConverter : JsonConverter
    {
        private readonly IScopeStore _scopeStore;

        public ScopeConverter(IScopeStore scopeStore)
        {
            if (scopeStore == null) throw new ArgumentNullException(nameof(scopeStore));

            this._scopeStore = scopeStore;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Scope) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ScopeLite>(reader);
            return _scopeStore.FindScopesAsync(new string[] { source.Name }).Result.Single();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (Scope)value;

            var target = new ScopeLite
            {
                Name = source.Name
            };

            serializer.Serialize(writer, target);
        }
    }
}
