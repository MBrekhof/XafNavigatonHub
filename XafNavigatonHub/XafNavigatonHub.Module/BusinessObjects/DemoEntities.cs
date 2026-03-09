using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace XafNavigatonHub.Module.BusinessObjects;

[DefaultClassOptions]
[ImageName("BO_Employee")]
public class Employee : BaseObject
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string Email { get; set; }
    public virtual string Department { get; set; }
    public virtual string JobTitle { get; set; }
    public virtual DateTime HireDate { get; set; }
    public virtual decimal Salary { get; set; }
    public virtual bool IsActive { get; set; } = true;
}

[DefaultClassOptions]
[ImageName("BO_Department")]
public class Department : BaseObject
{
    public virtual string Name { get; set; }
    public virtual string Code { get; set; }
    public virtual string Manager { get; set; }
    public virtual string Location { get; set; }
}

[DefaultClassOptions]
[ImageName("BO_Product")]
public class Product : BaseObject
{
    public virtual string Name { get; set; }
    public virtual string Sku { get; set; }
    public virtual string Category { get; set; }
    public virtual decimal Price { get; set; }
    public virtual int StockQuantity { get; set; }
    public virtual bool IsDiscontinued { get; set; }
}

[DefaultClassOptions]
[ImageName("BO_Customer")]
public class Customer : BaseObject
{
    public virtual string CompanyName { get; set; }
    public virtual string ContactName { get; set; }
    public virtual string Email { get; set; }
    public virtual string Phone { get; set; }
    public virtual string City { get; set; }
    public virtual string Country { get; set; }
}

[DefaultClassOptions]
[ImageName("BO_Order")]
public class SalesOrder : BaseObject
{
    public virtual string OrderNumber { get; set; }
    public virtual DateTime OrderDate { get; set; }
    public virtual string CustomerName { get; set; }
    public virtual decimal TotalAmount { get; set; }
    public virtual OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

[DefaultClassOptions]
[ImageName("BO_Task")]
public class ProjectTask : BaseObject
{
    public virtual string Subject { get; set; }
    public virtual string Description { get; set; }
    public virtual string AssignedTo { get; set; }
    public virtual DateTime DueDate { get; set; }
    public virtual TaskPriority Priority { get; set; }
    public virtual TaskState State { get; set; }
}

public enum TaskPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum TaskState
{
    NotStarted,
    InProgress,
    Completed,
    Deferred
}

[DefaultClassOptions]
[ImageName("BO_Audit_ChangeHistory")]
public class AuditLogEntry : BaseObject
{
    public virtual DateTime Timestamp { get; set; }
    public virtual string UserName { get; set; }
    public virtual string Action { get; set; }
    public virtual string EntityType { get; set; }
    public virtual string Details { get; set; }
}
