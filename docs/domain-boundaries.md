# Domain consistency boundaries

`AggregateRoot` extends `BaseEntity` and implements `IAggregateRoot<Guid>`.
`BaseEntity` represents identity, audit and soft-delete state shared by roots and
children. Only roots satisfy `IRepository<T>` and `Repository<T>` constraints.
`ISoftDeletable` exposes deletion state; public deletion/restoration commands live
on roots. Audit operations remain available to infrastructure.

Entities with their own repositories keep independent lifecycles, even if another
root has a navigation to them. Examples include `UserSession`, `UserToken`,
`HumanContract`, `HumanAttendanceRecord`, and `Machine`. Other independent master
data also derives from `AggregateRoot`. A navigation alone does not establish
ownership. Owned rows such as `OrderItem`, `DocumentItem`, `InvoiceItem`,
`Payment`, `Refund`, `ProjectPhase` and `ProjectTask` remain `BaseEntity` objects.

## Working with owned rows

Every collection returns a read-only view instead of its backing list. This also
applies to primitive lists and domain event buffers. Casting a view to `IList`
does not permit changes. EF collection navigations explicitly use their backing
fields, so loading a graph does not depend on a writable public collection.

The root checks the child's owner identifier and rejects duplicate identities
before accepting an externally constructed child. Removing a child checks its
membership. Constructors can still assemble detached rows, but changes to attached
rows are exposed through typed root commands in `*.Children.cs`:

```csharp
var line = new OrderItem(order, item, 1, price, 2m, unitOfMeasureId);
order.AddItem(line);
order.SetOrderItemDiscount(line.Id, discount);
```

Child mutation methods are internal to the Domain assembly. Root commands select
the actual child from the root's collection by identifier; a detached or foreign
identifier cannot change another graph. Nested project tasks, payment refunds and
production task assignments are reached through their root's collections too.
Draft/pending guards protect line configuration. Payment, receipt, delivery and
other lifecycle operations have their own eligibility rules. Project and
production child commands reject terminal roots. Changes to default addresses,
primary contacts and preferred suppliers coordinate the siblings.

`Document.ChangeStatus(Definite)` uses `Post()` and its balance/year checks;
returning a posted document to draft is rejected. Transfer dispatch records the
quantity and actual cost in one validated root command. Purchase receipt changes
refresh the root status, including reversing a completely received order.

Domain events intentionally remain on `BaseEntity`: facts raised by owned rows
(e.g. a payment or milestone) still participate in the existing Outbox pipeline.
`MainDbContext` collects events from all tracked entities, so separating the root
marker does not discard child events.

## Validation and atomic changes

Required text rejects whitespace before trimming. Currency precision and daily
hour limits use the same validation in construction and updates. Document date
updates validate the financial year and recompute the accounting period. HR
education and work-experience dates use shared chronology checks. A shorter
payment term cannot invalidate an existing early-discount window.

Value parsing, date arithmetic and money calculations that can fail are performed
before state is assigned. This includes payment-term references, contact numbers,
legal identifiers, production and maintenance costs, inventory values, planning
suggestions, warranty/certificate dates and installment schedules.

`Order.SetItems`, `Invoice.SetItems`, `PriceList.SetItems` and weekend replacement
materialize and validate their input before replacing existing state. Price list
merge/replacement validates the complete resulting set, so a duplicate or foreign
line cannot leave the list partly replaced. A configured detached `PriceListItem`
is built using constructor arguments before the root accepts it.

## Immutable snapshots

`Address`, `ShipToAddress` and `Image` have no public mutating methods. Address
and shipping `With...` methods return validated new values. Equality includes all
persisted address references and delivery instructions. Coordinates use invariant
culture and the existing `GeoCoordinate` ranges.

Owned address/image values are copied at entity boundaries. This preserves value
semantics when a caller shares one input across multiple entities while giving
EF a distinct owned instance per persisted owner. Profile image deletion clears
an optional reference and is idempotent; it never empties a shared `Image`.
Existing private setters/parameterless constructors remain for EF materialization.

## Verification

Use .NET 10 and the package versions in the checked-in project files:

```bash
dotnet restore src/ERP.sln --disable-parallel
dotnet build src/ERP.sln --no-restore -m:1
dotnet src/test/ERP.Test.xUnit/bin/Debug/net10.0/ERP.Test.xUnit.dll \
  -noColor -maxThreads 4 -result-xml tests.xml -result-trx tests.trx
```

The existing xUnit v3 executable is used directly, avoiding the repository's
existing VSTest/Microsoft.Testing.Platform runner configuration mismatch.
New regression coverage includes repository constraints, collection protection,
foreign/duplicate children, terminal roots, atomic failures, immutable values,
price-list handlers, EF model access modes, reload/update of owned snapshots and
loading a milestone with its owning aggregate. EF round-trip tests use InMemory;
the existing suite also builds and checks SQL Server relational metadata and
query translation. These checks do not require a running production database.

The SQL Server relational model was also compared to the checked-in migration
snapshot with `IMigrationsModelDiffer`: zero schema operations. These changes do
not require a database migration.
