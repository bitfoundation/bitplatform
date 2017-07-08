using Bit.OData.ODataControllers;
using BitChangeSetManager.DataAccess;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;

namespace BitChangeSetManager.Api
{
    public class DeliveriesController : DefaultDtoSetController<Delivery, DeliveryDto>
    {
        public DeliveriesController(IBitChangeSetManagerRepository<Delivery> deliveriesRepository)
            : base(deliveriesRepository)
        {

        }
    }
}