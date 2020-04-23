/// <reference path="node_modules/@bit/bitframework/typings.all.d.ts" />

declare module BitChangeSetManager.Dto {
	
	class UserDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			UserName : string;
			static UserName : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ProvinceDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Name : string;
			static Name : any;
				    
			CitiesCount : string;
			static CitiesCount : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ConstantDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Name : string;
			static Name : any;
				    
			Title : string;
			static Title : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class CustomerDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Name : string;
			static Name : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ChangeSetDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Title : string;
			static Title : any;
				    
			AssociatedCommitUrl : string;
			static AssociatedCommitUrl : any;
				    
			CreatedOn : Date;
			static CreatedOn : any;
				    
			Description : string;
			static Description : any;
				    
			IsDeliveredToAll : boolean;
			static IsDeliveredToAll : any;
				    
			SeverityId : string;
			static SeverityId : any;
				    
			SeverityTitle : string;
			static SeverityTitle : any;
				    
			DeliveryRequirementId : string;
			static DeliveryRequirementId : any;
				    
			DeliveryRequirementTitle : string;
			static DeliveryRequirementTitle : any;
				    
			CityId : string;
			static CityId : any;
				    
			CityName : string;
			static CityName : any;
				    
			ProvinceId : string;
			static ProvinceId : any;
				    
			ProvinceName : string;
			static ProvinceName : any;
				    
			NeedsReviewId : string;
			static NeedsReviewId : any;
				    
			Images : Array<BitChangeSetManager.Dto.ChangeSetImagetDto>;
			static Images : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class DeliveryDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			CustomerId : string;
			static CustomerId : any;
				    
			CustomerName : string;
			static CustomerName : any;
				    
			ChangeSetId : string;
			static ChangeSetId : any;
				    
			ChangeSetTitle : string;
			static ChangeSetTitle : any;
				    
			DeliveredOn : Date;
			static DeliveredOn : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class CityDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Name : string;
			static Name : any;
				    
			ProvinceId : string;
			static ProvinceId : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ChangeSetSeverityDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Title : string;
			static Title : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ChangeSetDescriptionTemplateDto extends $data.Entity {
				    
			Title : string;
			static Title : any;
				    
			Content : string;
			static Content : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ChangeSetDeliveryRequirementDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Title : string;
			static Title : any;
			}
}


declare module BitChangeSetManager.Dto {
	
	class ChangeSetImagetDto extends $data.Entity {
				    
			Id : string;
			static Id : any;
				    
			Name : string;
			static Name : any;
				    
			ChangeSetId : string;
			static ChangeSetId : any;
				    
			ChangeSet : BitChangeSetManager.Dto.ChangeSetDto;
			static ChangeSet : any;
			}
}




    
	interface ChangeSetImagesEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ChangeSetImagetDto>{
			}
    
	interface ChangeSetDeliveryRequirementsEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ChangeSetDeliveryRequirementDto>{
			}
    
	interface ChangeSetDescriptionTemplateEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto>{
				    
		    getAllTemplates():  $data.Queryable<BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto> ;
			}
    
	interface ChangeSetSeveritiesEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ChangeSetSeverityDto>{
			}
    
	interface CitiesEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.CityDto>{
			}
    
	interface DeliveriesEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.DeliveryDto>{
			}
    
	interface ChangeSetsEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ChangeSetDto>{
			}
    
	interface CustomersEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.CustomerDto>{
			}
    
	interface ConstantsEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ConstantDto>{
			}
    
	interface ProvincesEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.ProvinceDto>{
			}
    
	interface UsersEntitySet extends $data.EntitySet<BitChangeSetManager.Dto.UserDto>{
				    
		    getCurrentUser():  $data.Queryable<BitChangeSetManager.Dto.UserDto> ;
			}

declare class BitChangeSetManagerContext extends $data.EntityContext {

		    
		changeSetImages: ChangeSetImagesEntitySet;
		    
		changeSetDeliveryRequirements: ChangeSetDeliveryRequirementsEntitySet;
		    
		changeSetDescriptionTemplate: ChangeSetDescriptionTemplateEntitySet;
		    
		changeSetSeverities: ChangeSetSeveritiesEntitySet;
		    
		cities: CitiesEntitySet;
		    
		deliveries: DeliveriesEntitySet;
		    
		changeSets: ChangeSetsEntitySet;
		    
		customers: CustomersEntitySet;
		    
		constants: ConstantsEntitySet;
		    
		provinces: ProvincesEntitySet;
		    
		users: UsersEntitySet;
	
}

	import BitChangeSetManagerModel = BitChangeSetManager.Dto;

