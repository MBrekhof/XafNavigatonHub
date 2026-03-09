# How to Implement

Guide for contributors and users working with this XAF application.

## Adding a Business Object

1. Create a new class in `XafNavigatonHub.Module/BusinessObjects/`
2. Inherit from `DevExpress.Persistent.BaseImpl.EF.BaseObject` (or another XAF base class)
3. Add a `DbSet<YourEntity>` to `XafNavigatonHubEFCoreDbContext`
4. XAF auto-generates CRUD views — no manual UI code needed

```csharp
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace XafNavigatonHub.Module.BusinessObjects;

[DefaultClassOptions] // Makes it visible in navigation
public class MyEntity : BaseObject
{
    public virtual string Name { get; set; }
}
```

## Adding a Controller

Place platform-agnostic controllers in `XafNavigatonHub.Module/Controllers/`. For platform-specific controllers, use the respective frontend project's `Controllers/` folder.

```csharp
using DevExpress.ExpressApp;

namespace XafNavigatonHub.Module.Controllers;

public class MyController : ViewController
{
    public MyController()
    {
        TargetObjectType = typeof(MyEntity);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        // Add logic here
    }
}
```

## Database Seeding

Add seed data in `XafNavigatonHub.Module/DatabaseUpdate/Updater.cs` inside `UpdateDatabaseAfterUpdateSchema()`.

## Security & Permissions

- Roles and permissions are managed via `PermissionPolicyRole`
- Default roles (Admin, Default) are created in the `Updater`
- To restrict access to a new business object, add type/object/member permissions to the relevant role

## Model Customization

- Use the XAF Model Editor (Visual Studio designer) to customize views
- Platform-agnostic changes: `XafNavigatonHub.Module/Model.DesignedDiffs.xafml`
- Platform-specific changes: `Model.xafml` in each frontend project
