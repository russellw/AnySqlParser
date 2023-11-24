namespace AnySqlParser;
public sealed class PartitionSchemeRef: StorageOption {
	public string PartitionSchemeName;
	public string PartitionColumnName;

	public PartitionSchemeRef(Location location, string partitionSchemeName, string partitionColumnName): base(location) {
		PartitionSchemeName = partitionSchemeName;
		PartitionColumnName = partitionColumnName;
	}
}
