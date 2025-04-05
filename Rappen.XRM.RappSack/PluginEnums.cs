namespace Rappen.XRM.RappSack
{
    public enum ProcessingStage
    {
        PreValidation = 10,
        PreOperation = 20,
        MainOperation = 30,
        PostOperation = 40
    }

    public enum ProcessingMode
    {
        Synchronous = 0,
        Asynchronous = 1
    }

    public enum MessageName
    {
        AddItem,
        AddListMembers,
        AddMember,
        AddMembers,
        AddPrincipalToQueue,
        AddPrivileges,
        AddProductToKit,
        AddRecurrence,
        AddToQueue,
        AddUserToRecordTeam,
        ApplyRecordCreationAndUpdateRule,
        Assign,
        Associate,
        BackgroundSend,
        Book,
        CalculatePrice,
        Cancel,
        CheckIncoming,
        CheckPromote,
        Clone,
        CloneMobileOfflineProfile,
        CloneProduct,
        Close,
        CopyDynamicListToStatic,
        CopySystemForm,
        Create,
        CreateException,
        CreateInstance,
        CreateKnowledgeArticleTranslation,
        CreateKnowledgeArticleVersion,
        Delete,
        DeleteOpenInstances,
        DeliverIncoming,
        DeliverPromote,
        Disassociate,
        Execute,
        ExecuteById,
        Export,
        GenerateSocialProfile,
        GetDefaultPriceLevel,
        GrantAccess,
        Import,
        LockInvoicePricing,
        LockSalesOrderPricing,
        Lose,
        Merge,
        ModifyAccess,
        PickFromQueue,
        Publish,
        PublishAll,
        PublishTheme,
        QualifyLead,
        Recalculate,
        ReleaseToQueue,
        RemoveFromQueue,
        RemoveItem,
        RemoveMember,
        RemoveMembers,
        RemovePrivilege,
        RemoveProductFromKit,
        RemoveRelated,
        RemoveUserFromRecordTeam,
        ReplacePrivileges,
        Reschedule,
        Retrieve,
        RetrieveExchangeRate,
        RetrieveFilteredForms,
        RetrieveMultiple,
        RetrievePersonalWall,
        RetrievePrincipalAccess,
        RetrieveRecordWall,
        RetrieveSharedPrincipalsAndAccess,
        RetrieveUnpublished,
        RetrieveUnpublishedMultiple,
        RetrieveUserQueues,
        RevokeAccess,
        RouteTo,
        Send,
        SendFromTemplate,
        SetLocLabels,
        SetRelated,
        SetState,
        TriggerServiceEndpointCheck,
        UnlockInvoicePricing,
        UnlockSalesOrderPricing,
        Update,
        ValidateRecurrenceRule,
        Win
    }

    /// <summary>
    /// Common parameter names for InputParameters and OutputParameters in plugin steps
    /// </summary>
    public static class ParameterName
    {
        public const string Assignee = "Assignee";
        public const string AsyncOperationId = "AsyncOperationId";
        public const string BusinessEntity = "BusinessEntity";
        public const string BusinessEntityCollection = "BusinessEntityCollection";
        public const string CampaignActivityId = "CampaignActivityId";
        public const string CampaignId = "CampaignId";
        public const string ColumnSet = "ColumnSet";
        public const string Context = "context";
        public const string ContractId = "ContractId";
        public const string EmailId = "emailid";
        public const string EndpointId = "EndpointId";
        public const string EntityId = "EntityId";
        public const string EntityMoniker = "EntityMoniker";
        public const string ExchangeRate = "ExchangeRate";
        public const string FaxId = "FaxId";
        public const string FetchXml = "FetchXml";
        public const string Id = "id";
        public const string IncidentResolution = "IncidentResolution";
        public const string ListId = "ListId";
        public const string OptionalParameters = "OptionalParameters";
        public const string PostBusinessEntity = "PostBusinessEntity";
        public const string PostMasterBusinessEntity = "PostMasterBusinessEntity";
        public const string PreBusinessEntity = "PreBusinessEntity";
        public const string PreMasterBusinessEntity = "PreMasterBusinessEntity";
        public const string PreSubordinateBusinessEntity = "PreSubordinateBusinessEntity";
        public const string Query = "Query";
        public const string RelatedEntities = "RelatedEntities";
        public const string Relationship = "Relationship";
        public const string ReturnDynamicEntities = "ReturnDynamicEntities";
        public const string RouteType = "RouteType";
        public const string Settings = "Settings";
        public const string State = "State";
        public const string Status = "Status";
        public const string SubordinateId = "subordinateid";
        public const string Target = "Target";
        public const string Targets = "Targets";
        public const string TeamId = "TeamId";
        public const string TemplateId = "TemplateId";
        public const string TriggerAttribute = "TriggerAttribute";
        public const string UpdateContent = "UpdateContent";
        public const string ValidationResult = "ValidationResult";
        public const string WorkflowId = "WorkflowId";
    }
}