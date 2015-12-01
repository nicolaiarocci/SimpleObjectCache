using System;

#if __PORTABLE__
using SQLite.Net.Attributes;
#else
using SQLite;
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
