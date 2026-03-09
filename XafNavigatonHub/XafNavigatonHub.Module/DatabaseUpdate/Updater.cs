using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Microsoft.Extensions.DependencyInjection;
using XafNavigatonHub.Module.BusinessObjects;

namespace XafNavigatonHub.Module.DatabaseUpdate
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Updating.ModuleUpdater
    public class Updater : ModuleUpdater
    {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) :
            base(objectSpace, currentDBVersion)
        {
        }
        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();
            //string name = "MyName";
            //EntityObject1 theObject = ObjectSpace.FirstOrDefault<EntityObject1>(u => u.Name == name);
            //if(theObject == null) {
            //    theObject = ObjectSpace.CreateObject<EntityObject1>();
            //    theObject.Name = name;
            //}

            // The code below creates users and roles for testing purposes only.
            // In production code, you can create users and assign roles to them automatically, as described in the following help topic:
            // https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication
#if !RELEASE
            // If a role doesn't exist in the database, create this role
            var defaultRole = CreateDefaultRole();
            var adminRole = CreateAdminRole();

            ObjectSpace.CommitChanges(); //This line persists created object(s).

            UserManager userManager = ObjectSpace.ServiceProvider.GetRequiredService<UserManager>();

            // If a user named 'User' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "User") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "User", EmptyPassword, (user) =>
                {
                    // Add the Users role to the user
                    user.Roles.Add(defaultRole);
                });
            }

            // If a user named 'Admin' doesn't exist in the database, create this user
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "Admin") == null)
            {
                // Set a password if the standard authentication type is used
                string EmptyPassword = "";
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "Admin", EmptyPassword, (user) =>
                {
                    // Add the Administrators role to the user
                    user.Roles.Add(adminRole);
                });
            }

            // Create HR Manager role with limited access
            var hrRole = CreateHrRole();

            // Create Sales role
            var salesRole = CreateSalesRole();

            // Create demo users
            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "HrManager") == null)
            {
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "HrManager", "", (user) =>
                {
                    user.Roles.Add(defaultRole);
                    user.Roles.Add(hrRole);
                });
            }

            if (userManager.FindUserByName<ApplicationUser>(ObjectSpace, "SalesRep") == null)
            {
                _ = userManager.CreateUser<ApplicationUser>(ObjectSpace, "SalesRep", "", (user) =>
                {
                    user.Roles.Add(defaultRole);
                    user.Roles.Add(salesRole);
                });
            }

            ObjectSpace.CommitChanges();

            SeedDemoData();

            ObjectSpace.CommitChanges(); //This line persists created object(s).
#endif
        }
        public override void UpdateDatabaseBeforeUpdateSchema()
        {
            base.UpdateDatabaseBeforeUpdateSchema();
        }
        PermissionPolicyRole CreateAdminRole()
        {
            PermissionPolicyRole adminRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Administrators");
            if (adminRole == null)
            {
                adminRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                adminRole.Name = "Administrators";
                adminRole.IsAdministrative = true;
            }
            return adminRole;
        }
        PermissionPolicyRole CreateHrRole()
        {
            var role = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "HR");
            if (role == null)
            {
                role = ObjectSpace.CreateObject<PermissionPolicyRole>();
                role.Name = "HR";
                role.AddTypePermissionsRecursively<Employee>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                role.AddTypePermissionsRecursively<Department>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                role.AddTypePermissionsRecursively<ProjectTask>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                role.AddTypePermissionsRecursively<AuditLogEntry>(SecurityOperations.Read, SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/Employee_ListView", SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/Department_ListView", SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/ProjectTask_ListView", SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/AuditLogEntry_ListView", SecurityPermissionState.Allow);
            }
            return role;
        }

        PermissionPolicyRole CreateSalesRole()
        {
            var role = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Sales");
            if (role == null)
            {
                role = ObjectSpace.CreateObject<PermissionPolicyRole>();
                role.Name = "Sales";
                role.AddTypePermissionsRecursively<Customer>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                role.AddTypePermissionsRecursively<SalesOrder>(SecurityOperations.CRUDAccess, SecurityPermissionState.Allow);
                role.AddTypePermissionsRecursively<Product>(SecurityOperations.Read, SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/Customer_ListView", SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/SalesOrder_ListView", SecurityPermissionState.Allow);
                role.AddNavigationPermission("Application/NavigationItems/Items/Default/Items/Product_ListView", SecurityPermissionState.Allow);
            }
            return role;
        }

        void SeedDemoData()
        {
            if (ObjectSpace.GetObjectsCount(typeof(Employee), null) > 0) return;

            // Departments
            var departments = new[] { "Engineering", "Sales", "HR", "Marketing", "Finance" };
            foreach (var name in departments)
            {
                var dept = ObjectSpace.CreateObject<Department>();
                dept.Name = name;
                dept.Code = name[..Math.Min(3, name.Length)].ToUpper();
                dept.Manager = name switch
                {
                    "Engineering" => "Alice Chen",
                    "Sales" => "Bob Martinez",
                    "HR" => "Carol Williams",
                    "Marketing" => "David Kim",
                    "Finance" => "Eva Singh",
                    _ => ""
                };
                dept.Location = name switch
                {
                    "Engineering" => "Building A, Floor 3",
                    "Sales" => "Building B, Floor 1",
                    "HR" => "Building A, Floor 1",
                    "Marketing" => "Building B, Floor 2",
                    "Finance" => "Building A, Floor 2",
                    _ => ""
                };
            }

            // Employees
            var employees = new (string First, string Last, string Dept, string Title, decimal Salary)[]
            {
                ("Alice", "Chen", "Engineering", "VP Engineering", 185000),
                ("James", "Park", "Engineering", "Senior Developer", 145000),
                ("Maria", "Garcia", "Engineering", "Developer", 110000),
                ("Tom", "Brown", "Engineering", "Junior Developer", 85000),
                ("Bob", "Martinez", "Sales", "Sales Director", 160000),
                ("Sarah", "Johnson", "Sales", "Account Executive", 95000),
                ("Mike", "Davis", "Sales", "Sales Rep", 75000),
                ("Carol", "Williams", "HR", "HR Director", 140000),
                ("Lisa", "Taylor", "HR", "Recruiter", 80000),
                ("David", "Kim", "Marketing", "Marketing Manager", 120000),
                ("Emma", "Wilson", "Marketing", "Content Specialist", 70000),
                ("Eva", "Singh", "Finance", "CFO", 175000),
                ("Ryan", "Lee", "Finance", "Accountant", 90000),
            };
            foreach (var (first, last, dept, title, salary) in employees)
            {
                var emp = ObjectSpace.CreateObject<Employee>();
                emp.FirstName = first;
                emp.LastName = last;
                emp.Email = $"{first.ToLower()}.{last.ToLower()}@example.com";
                emp.Department = dept;
                emp.JobTitle = title;
                emp.HireDate = DateTime.Today.AddDays(-Random.Shared.Next(100, 1500));
                emp.Salary = salary;
            }

            // Customers
            var customers = new (string Company, string Contact, string City, string Country)[]
            {
                ("Acme Corp", "John Smith", "New York", "USA"),
                ("TechStart GmbH", "Hans Mueller", "Berlin", "Germany"),
                ("Sakura Industries", "Yuki Tanaka", "Tokyo", "Japan"),
                ("Nordic Solutions", "Lars Eriksson", "Stockholm", "Sweden"),
                ("Southern Star Ltd", "Maria Santos", "São Paulo", "Brazil"),
                ("Atlas Consulting", "Priya Patel", "Mumbai", "India"),
                ("Maple Systems", "Jean Tremblay", "Montreal", "Canada"),
                ("Blue Ocean Co", "Wei Zhang", "Shanghai", "China"),
            };
            foreach (var (company, contact, city, country) in customers)
            {
                var cust = ObjectSpace.CreateObject<Customer>();
                cust.CompanyName = company;
                cust.ContactName = contact;
                cust.Email = $"{contact.Split(' ')[0].ToLower()}@{company.ToLower().Replace(" ", "")}.com";
                cust.Phone = $"+1-555-{Random.Shared.Next(1000, 9999)}";
                cust.City = city;
                cust.Country = country;
            }

            // Products
            var products = new (string Name, string Sku, string Cat, decimal Price, int Stock)[]
            {
                ("Widget Pro", "WGT-001", "Hardware", 29.99m, 500),
                ("Gadget Plus", "GDG-002", "Hardware", 49.99m, 250),
                ("SoftSuite License", "SST-010", "Software", 199.99m, 9999),
                ("Cloud Basic Plan", "CLD-020", "Services", 9.99m, 9999),
                ("Cloud Pro Plan", "CLD-021", "Services", 29.99m, 9999),
                ("Enterprise Bundle", "ENT-100", "Bundles", 999.99m, 100),
                ("Support Pack", "SUP-050", "Services", 149.99m, 9999),
                ("Training Course", "TRN-030", "Education", 399.99m, 50),
            };
            foreach (var (name, sku, cat, price, stock) in products)
            {
                var prod = ObjectSpace.CreateObject<Product>();
                prod.Name = name;
                prod.Sku = sku;
                prod.Category = cat;
                prod.Price = price;
                prod.StockQuantity = stock;
            }

            // Sales Orders
            var statuses = Enum.GetValues<OrderStatus>();
            for (int i = 1; i <= 15; i++)
            {
                var order = ObjectSpace.CreateObject<SalesOrder>();
                order.OrderNumber = $"SO-{2025000 + i}";
                order.OrderDate = DateTime.Today.AddDays(-Random.Shared.Next(1, 90));
                order.CustomerName = customers[Random.Shared.Next(customers.Length)].Company;
                order.TotalAmount = Math.Round((decimal)(Random.Shared.NextDouble() * 5000 + 100), 2);
                order.Status = statuses[Random.Shared.Next(statuses.Length)];
            }

            // Tasks
            var taskSubjects = new[]
            {
                "Onboard new developer", "Update employee handbook", "Quarterly review prep",
                "Fix login page bug", "Deploy v2.1 to staging", "Customer demo preparation",
                "Update pricing page", "Write Q3 blog post", "Audit expense reports",
                "Renew vendor contracts", "Plan team offsite", "Security audit follow-up",
            };
            var assignees = new[] { "Alice Chen", "James Park", "Bob Martinez", "Carol Williams", "David Kim", "Eva Singh" };
            foreach (var subject in taskSubjects)
            {
                var task = ObjectSpace.CreateObject<ProjectTask>();
                task.Subject = subject;
                task.AssignedTo = assignees[Random.Shared.Next(assignees.Length)];
                task.DueDate = DateTime.Today.AddDays(Random.Shared.Next(-10, 30));
                task.Priority = Enum.GetValues<TaskPriority>()[Random.Shared.Next(4)];
                task.State = Enum.GetValues<TaskState>()[Random.Shared.Next(4)];
            }

            // Audit log entries
            var actions = new[] { "Login", "Create", "Update", "Delete", "Export" };
            var entities = new[] { "Employee", "Customer", "SalesOrder", "Product", "ProjectTask" };
            for (int i = 0; i < 20; i++)
            {
                var entry = ObjectSpace.CreateObject<AuditLogEntry>();
                entry.Timestamp = DateTime.Now.AddMinutes(-Random.Shared.Next(1, 10000));
                entry.UserName = new[] { "Admin", "HrManager", "SalesRep", "User" }[Random.Shared.Next(4)];
                entry.Action = actions[Random.Shared.Next(actions.Length)];
                entry.EntityType = entities[Random.Shared.Next(entities.Length)];
                entry.Details = $"{entry.Action} on {entry.EntityType} by {entry.UserName}";
            }
        }

        PermissionPolicyRole CreateDefaultRole()
        {
            PermissionPolicyRole defaultRole = ObjectSpace.FirstOrDefault<PermissionPolicyRole>(role => role.Name == "Default");
            if (defaultRole == null)
            {
                defaultRole = ObjectSpace.CreateObject<PermissionPolicyRole>();
                defaultRole.Name = "Default";

                defaultRole.AddObjectPermissionFromLambda<ApplicationUser>(SecurityOperations.Read, cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddNavigationPermission(@"Application/NavigationItems/Items/Default/Items/MyDetails", SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "ChangePasswordOnFirstLogon", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(SecurityOperations.Write, "StoredPassword", cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(), SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<PermissionPolicyRole>(SecurityOperations.Read, SecurityPermissionState.Deny);
                defaultRole.AddObjectPermission<ModelDifference>(SecurityOperations.ReadWriteAccess, "UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddObjectPermission<ModelDifferenceAspect>(SecurityOperations.ReadWriteAccess, "Owner.UserId = ToStr(CurrentUserId())", SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifference>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ModelDifferenceAspect>(SecurityOperations.Create, SecurityPermissionState.Allow);
                defaultRole.AddObjectPermissionFromLambda<UserHubPreference>(
                    SecurityOperations.ReadWriteAccess,
                    p => p.UserId == (Guid)CurrentUserIdOperator.CurrentUserId(),
                    SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<UserHubPreference>(
                    SecurityOperations.Create, SecurityPermissionState.Allow);
                // Read access to demo entities for all users
                defaultRole.AddTypePermissionsRecursively<Employee>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<Department>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<Customer>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<SalesOrder>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<Product>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<ProjectTask>(SecurityOperations.Read, SecurityPermissionState.Allow);
                defaultRole.AddTypePermissionsRecursively<AuditLogEntry>(SecurityOperations.Read, SecurityPermissionState.Allow);
            }
            return defaultRole;
        }
    }
}
