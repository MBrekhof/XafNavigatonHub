using DevExpress.Persistent.BaseImpl.EF;

namespace XafNavigatonHub.Module.BusinessObjects;

public class UserHubPreference : BaseObject
{
    public virtual Guid UserId { get; set; }
    public virtual string NavigationItemId { get; set; }
    public virtual int SortOrder { get; set; }
}
