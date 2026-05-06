using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Users.Shared.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum TenantType
{
    Node,

    Leaf
}
