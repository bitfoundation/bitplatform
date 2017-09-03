using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;

namespace BitChangeSetManager.Api
{
    public class DeliveriesController : BitChangeSetManagerDtoSetController<DeliveryDto, Delivery, Guid>
    {
        public DeliveriesController(IBitChangeSetManagerRepository<Delivery> deliveriesRepository)
            : base(deliveriesRepository)
        {

        }
    }
}