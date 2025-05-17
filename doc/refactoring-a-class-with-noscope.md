# Refactoring a god class with NoScope

## Initial state

Let's say, you have a spaghetti class:

```csharp
class SpaghettiCart
{
    
    private IList<Item> Items {get;set;} = new List<Item>();

    public decimal TaxAmount => Items
        .Sum(a => taxService.GetRate(item.CategoryCode) * Item.Price);

    public decimal TotalAmount => Items.Sum(a=>a.Price) + TaxAmount;

    public void AddItem(Item item) => Items.Add(item);

    public void RecreateCart() => Items = new List<Item>();

    public string CartDescription
    {
        get
        {
            if (!items.Any())
            {
                return "You cart's empty";
            }
            return $"Your order is ${TotalAmount}(including tax of ${TaxAmount})";
        }
    }
}
```
 
You can split it into separate types

```csharp


// extract cart management
interface ICartManagementScope : IFacade<SpaghettiOrder>
{
    // we expose mutable IList as is
    IList<Item> Items {get;set;} 
}
class CartManagementService(ICartManagementScope scope)
{
    public void AddItem(Item item) => scope.Items.Add(item);

    public void RecreateCart() => scope.Items = new List<Item>();
}

// extract descriptions
interface ICartDescriptionScope : IFacade<SpaghettiOrder>
{
    decimal TotalAmount {get;}
    decimal TaxAmount {get;}

    protected IList<Item> Items {get;}
    // expose simple property without providing access to the underlying collection
    bool HasItems => Items.Any();
}
class CartDescriptionService
{
    public string BuildCartDescription(ICartDescriptionScope scope)
    {
         if (!scope.HasItems)
        {
            return "You cart's empty";
        }
        return $"Your order is ${scope.TotalAmount}(including tax of ${scope.TaxAmount})";
    }
}

interface IOrderTaxCalculatorScope : IFacade<SpaghettiOrder>
{
    [FacadeMember(Name = "Items")]
    protected IList<Item> _items {get;} 

    // expose a read-only view for calculations
    IReadOnlyList<Item> Items => new ReadOnlyList<Item>(_items);
}
class TaxCalculator(IOrderTaxCalculatorScope scope)
{
    public decimal GetTaxAmount() => scope.Items
        .Sum(item => taxRateService.GetRate(item.CategoryCode) * item.Price)
}

class SpaghettiCart(
    CartManagementServiceFactory cartManagementServiceFactory,
    TaxCalculatorFactory taxCalculatorFactory,
    CartDescriptionService cartDescriptionService 
)
{
    private IList<Item> Items {get;set;} = new List<Item>();

    private CartManagementService cartManagementService = cartManagementServiceFactory.Create(new CartManagementScope(this));
    private TaxCalculator taxCalculator = taxCalculatorFactory.Create(new OrderTaxCalculatorScope(this));

    public decimal TaxAmount => taxCalculator.GetTaxAmount();
    public decimal TotalAmount => Items.Sum(a=>a.Price) + TaxAmount;

    // went away as well
    public void AddItem(Item item) => cartManagementService.Add(item);
    public void RecreateCart() => cartManagementService.RecreateCart();

    // went away
    public string CartDescription => cartDescriptionService.BuildCartDescription(new CartDescriptionScope(this));
}
```