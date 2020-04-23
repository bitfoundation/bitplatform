

		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.UserDto = $data.Entity.extend("BitChangeSetManager.Dto.UserDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		UserName: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ProvinceDto = $data.Entity.extend("BitChangeSetManager.Dto.ProvinceDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Name: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		CitiesCount: {
			"type": "Edm.Int64" , nullable: false
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ConstantDto = $data.Entity.extend("BitChangeSetManager.Dto.ConstantDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Name: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		Title: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.CustomerDto = $data.Entity.extend("BitChangeSetManager.Dto.CustomerDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Name: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ChangeSetDto = $data.Entity.extend("BitChangeSetManager.Dto.ChangeSetDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Title: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		AssociatedCommitUrl: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																		, $ViewType : "Url"
							},
	 
		CreatedOn: {
			"type": "Edm.DateTimeOffset" , nullable: true
			  , defaultValue: null
																			},
	 
		Description: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																		, $ViewType : "MultilineText"
							},
	 
		IsDeliveredToAll: {
			"type": "Edm.Boolean" , nullable: false
																			},
	 
		SeverityId: {
			"type": "Edm.Guid" , nullable: false
																			},
	 
		SeverityTitle: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		DeliveryRequirementId: {
			"type": "Edm.Guid" , nullable: false
																			},
	 
		DeliveryRequirementTitle: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		CityId: {
			"type": "Edm.Guid" , nullable: true
			  , defaultValue: null
																			},
	 
		CityName: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		ProvinceId: {
			"type": "Edm.Guid" , nullable: true
			  , defaultValue: null
																			},
	 
		ProvinceName: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		NeedsReviewId: {
			"type": "Edm.Guid" , nullable: true
			  , defaultValue: null
																			},
	 
		Images: {
			"type": "Array" , nullable: true
			  , defaultValue: []
						 , "elementType": "BitChangeSetManager.Dto.ChangeSetImagetDto"
						 , "inverseProperty": "ChangeSet"
													},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.DeliveryDto = $data.Entity.extend("BitChangeSetManager.Dto.DeliveryDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		CustomerId: {
			"type": "Edm.Guid" , nullable: false
																			},
	 
		CustomerName: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		ChangeSetId: {
			"type": "Edm.Guid" , nullable: false
																			},
	 
		ChangeSetTitle: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		DeliveredOn: {
			"type": "Edm.DateTimeOffset" , nullable: false
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.CityDto = $data.Entity.extend("BitChangeSetManager.Dto.CityDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Name: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		ProvinceId: {
			"type": "Edm.Guid" , nullable: false
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ChangeSetSeverityDto = $data.Entity.extend("BitChangeSetManager.Dto.ChangeSetSeverityDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Title: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto = $data.Entity.extend("BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto", {
	 
		Title: {
			"type": "Edm.String" , nullable: true
															, "key": true
			, "required" : false
			, "computed": true
										},
	 
		Content: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ChangeSetDeliveryRequirementDto = $data.Entity.extend("BitChangeSetManager.Dto.ChangeSetDeliveryRequirementDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Title: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
		});


		 var   BitChangeSetManager = BitChangeSetManager || {};
		 BitChangeSetManager.Dto = BitChangeSetManager.Dto || {};

BitChangeSetManager.Dto.ChangeSetImagetDto = $data.Entity.extend("BitChangeSetManager.Dto.ChangeSetImagetDto", {
	 
		Id: {
			"type": "Edm.Guid" , nullable: false
															, "key": true
			, "required" : true
			, "computed": true
										},
	 
		Name: {
			"type": "Edm.String" , nullable: true
			  , defaultValue: null
																			},
	 
		ChangeSetId: {
			"type": "Edm.Guid" , nullable: false
																			},
	 
		ChangeSet: {
			"type": "BitChangeSetManager.Dto.ChangeSetDto" , nullable: true
			  , defaultValue: null
									 , "inverseProperty": "Images"
													},
		});



BitChangeSetManagerContext = $data.EntityContext.extend("BitChangeSetManagerContext", {
			changeSetImages : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ChangeSetImagetDto",
					},
			changeSetDeliveryRequirements : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ChangeSetDeliveryRequirementDto",
					},
			changeSetDescriptionTemplate : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto",
							"actions": {
													"getAllTemplates": {
								"type": "$data.ServiceFunction",
																	"namespace": null,
																"returnType":  "$data.Queryable" ,
								 "elementType": "BitChangeSetManager.Dto.ChangeSetDescriptionTemplateDto", 
																	"params": [
																			]
						},
										}
					},
			changeSetSeverities : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ChangeSetSeverityDto",
					},
			cities : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.CityDto",
					},
			deliveries : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.DeliveryDto",
					},
			changeSets : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ChangeSetDto",
					},
			customers : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.CustomerDto",
					},
			constants : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ConstantDto",
					},
			provinces : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.ProvinceDto",
					},
			users : {
			"type": "$data.EntitySet",
			"elementType": "BitChangeSetManager.Dto.UserDto",
							"actions": {
													"getCurrentUser": {
								"type": "$data.ServiceFunction",
																	"namespace": null,
																"returnType":  "$data.Queryable" ,
								 "elementType": "BitChangeSetManager.Dto.UserDto", 
																	"params": [
																			]
						},
										}
					},
	});

	BitChangeSetManagerModel = BitChangeSetManager.Dto;

