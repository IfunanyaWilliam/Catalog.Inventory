using Catalog.Inventory.Service.DTOs;
using Catalog.Inventory.Service.Entities;

namespace Catalog.Inventory.Service
{
    public static class Extensions
    {
        public static InventoryItemDto AsDto(this InventoryItem item, string name, string description)
        {
            return new InventoryItemDto(item.CatalogItemId, name, description, item.Quantity, item.AcquiredDate);
        }
    }
}