namespace BrokenHelper.PacketHandlers
{
    public record EquipmentInfo(string Name, int? Quality, double? ParsedVal,
        int? OrnamentField, string? OrbCode, string? OrbName, int? OrbPrice);

}
