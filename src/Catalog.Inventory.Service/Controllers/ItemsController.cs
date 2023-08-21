using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Catalog.Common;
using Catalog.Inventory.Service.DTOs;
using Catalog.Inventory.Service.Entities;
using System.Linq;
using Catalog.Inventory.Service.Clients;

namespace Catalog.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _itemsRepository;
        private readonly CatalogClient _catalogClient; 

        public ItemsController(IRepository<InventoryItem> itemsRepository,
                               CatalogClient catalogClient)
        {
            _itemsRepository = itemsRepository;
            _catalogClient = catalogClient;
        }

        [HttpGet("userId")]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if(userId == Guid.Empty)
                return BadRequest($"{nameof(userId)} is null");
            
            var catalogItems = await _catalogClient.GetCatalogItemsAsync();
            var inventoryItemsEntities = await _itemsRepository.GetAllAsync(item => item.UserId == userId);
            
            var inventoryItemDtos = inventoryItemsEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItems.SingleOrDefault(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            });
            
            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await _itemsRepository
                        .GetAsync(item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

            if(inventoryItem == null)
            {
                inventoryItem = new InventoryItem
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _itemsRepository.CreateAsync(inventoryItem);
            }

            inventoryItem.Quantity += grantItemsDto.Quantity;
            await _itemsRepository .UpdateAsync(inventoryItem);

            return Ok();
        }
    }   
}