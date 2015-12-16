using System;

#if NET4
using SQLite;
#else
using SQLite.Net.Attributes;
#endif

namespace Amica.vNext
{
    class CacheElement
    {
	[PrimaryKey]
	public string Key { get; set; }
	[Indexed]
	public string TypeName { get; set; }
	public byte[] Value { get; set; }
	[Indexed]
	public DateTime? Expiration { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
    }
}
