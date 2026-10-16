namespace ERP.Domain.Entities.Modules.OrderManagement;

/// <summary>
/// Where the physical delivery of an order has got to, which moves independently of whether
/// the order has been paid for.
/// </summary>
public enum OrderShipmentStatus
{
    /// <summary>
    /// سفارش لغو شد
    /// </summary>
    OrderCancelled = 0,
    /// <summary>
    /// در انتظار پرداخت
    /// </summary>
    AwaitingPayment = 5,
    /// <summary>
    /// در انتظار بررسی سفارش
    /// </summary>
    InQueueReview = 15,
    /// <summary>
    /// سفارش تایید شد
    /// </summary>
    OrderConfirmed = 25,
    /// <summary>
    /// در حال آماده سازی سفارش
    /// </summary>
    OrderPreparation = 35,
    /// <summary>
    /// سفارش بسته بندی شده و آماده برای ارسال می باشد
    /// </summary>
    Prepared = 50,
    /// <summary>
    /// سفارش از مرکز پردازش فروشگاه خارج شد
    /// </summary>
    ExitedFromStoreProcessingCenter = 60,
    /// <summary>
    /// سفارش به مرکز توزیع تحویل داده شد
    /// </summary>
    ArrivedToDistributionCenter = 75,
    /// <summary>
    /// سفارش تحویل مامور پستی داده شد
    /// </summary>
    DeliveryToPostSenderAgent = 90,
    /// <summary>
    /// سفارش به مشتری تحویل داده شد
    /// </summary>
    DeliveryToCustomer = 100
}
