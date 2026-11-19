using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LabDelegatesLinq
{

    public delegate double MetricTransform(double baseValue, double factor);
    public delegate void AuditNotify(string stage, double currentValue);

    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            RunTask1();
            Console.WriteLine(new string('-', 60));
            RunTask2();
            Console.WriteLine(new string('-', 60));
            RunTask3();
            Console.WriteLine(new string('-', 60));
            RunTask4();
        }

        private static void RunTask1()
        {
            Console.WriteLine("=== ЗАВДАННЯ 1: Власні делегати та multicast ===");

            // 1. Три способи створення екземпляра делегата
            MetricTransform staticRef = PricingRules.ApplyLinearMarkup; // статичний метод
            var adjuster = new TaxAdjuster(0.05);
            MetricTransform instanceRef = adjuster.ApplyCompoundRate; // екземплярний метод
            MetricTransform lambdaRef = (val, k) => val * k + 15.0;   // лямбда-вираз

            Console.WriteLine($"Статичний метод: {staticRef(100.0, 1.2):F2}");
            Console.WriteLine($"Екземплярний метод: {instanceRef(100.0, 1.2):F2}");
            Console.WriteLine($"Лямбда-вираз: {lambdaRef(100.0, 1.2):F2}");

            // 2. Безпечний виклик непризначеного делегата
            AuditNotify? optionalHook = null;
            optionalHook?.Invoke("Init", 100.0); // не викидає NullReferenceException
            Console.WriteLine("Безпечний виклик null-делегата виконано без помилок.");

            // 3. Multicast типу void (додавання і вилучення підписників)
            int eventsCount = 0;
            AuditNotify voidChain = (stage, val) => Console.WriteLine($"  [1. Лог] Етап '{stage}': {val:F2}");
            voidChain += (stage, val) => { eventsCount++; Console.WriteLine($"  [2. Лічильник] Подій: {eventsCount}"); };
            AuditNotify thresholdCheck = (stage, val) => Console.WriteLine($"  [3. Поріг] Перевищено 110: {val > 110}");
            voidChain += thresholdCheck;

            Console.WriteLine("Виклик multicast (3 підписники):");
            voidChain?.Invoke("Розрахунок", 120.0);

            voidChain -= thresholdCheck; // вилучення третього обробника
            Console.WriteLine("Виклик multicast після видалення 3-го підписника:");
            voidChain?.Invoke("Фінал", 105.0);

            // 4. Multicast із типом повернення double: прямий виклик vs GetInvocationList()
            MetricTransform valueChain = staticRef;
            valueChain += instanceRef;
            valueChain += lambdaRef;

            double directResult = valueChain(100.0, 1.2);
            Console.WriteLine($"Прямий виклик multicast (повертає лише останній): {directResult:F2}");

            var allResults = new List<string>();
            foreach (MetricTransform handler in valueChain.GetInvocationList())
            {
                allResults.Add(handler(100.0, 1.2).ToString("F2"));
            }
            Console.WriteLine($"Усі результати через GetInvocationList(): [{string.Join("; ", allResults)}]");
        }
        private static void RunTask2()
        {
            Console.WriteLine("=== ЗАВДАННЯ 2: Лямбда-вирази та замикання ===");

            // 1. Набір форм лямбда-виразів
            Func<int> getCurrentYear = () => 2026;
            Func<double, double> square = x => x * x;
            Func<double, double, double> average = (a, b) => (a + b) / 2.0;
            Action<string> printBanner = msg =>
            {
                string clean = msg.Trim().ToUpperInvariant();
                Console.WriteLine($"  [Блочна лямбда]: {clean}");
            };

            Console.WriteLine($"Без параметрів: {getCurrentYear()}, 1 параметр: {square(5)}, 2 параметри: {average(10, 20)}");
            printBanner("  привіт із блочного тіла  ");

            // 2. Фабрика замикань із різною конфігурацією
            Func<double, double> standardDiscount = CreateDiscountCalculator(0.10, 500.0);
            Func<double, double> premiumDiscount = CreateDiscountCalculator(0.25, 1000.0);
            Console.WriteLine($"Замовлення 1200 грн -> стандартна ціна: {standardDiscount(1200):F2}, преміум: {premiumDiscount(1200):F2}");

            // 3. Пастка замикання в циклі vs виправлений варіант
            var trappedActions = new List<Func<int>>();
            for (int i = 1; i <= 3; i++)
            {
                trappedActions.Add(() => i); // захоплює одну спільну змінну i
            }
            Console.WriteLine($"Пастка циклу (очікували 1,2,3): {string.Join(", ", trappedActions.Select(f => f()))}");

            var fixedActions = new List<Func<int>>();
            for (int i = 1; i <= 3; i++)
            {
                int localCopy = i; // локальна копія на кожну ітерацію
                fixedActions.Add(() => localCopy);
            }
            Console.WriteLine($"Виправлений цикл: {string.Join(", ", fixedActions.Select(f => f()))}");

            // 4. Спостереження за зміною захопленого стану між викликами
            int sharedState = 10;
            Func<int> observer = () => sharedState * 2;
            Console.WriteLine($"До зміни стану: {observer()}");
            sharedState = 35;
            Console.WriteLine($"Після зміни захопленої змінної: {observer()}");
        }

        private static Func<double, double> CreateDiscountCalculator(double rate, double minThreshold)
        {
            return amount => amount >= minThreshold ? amount * (1.0 - rate) : amount;
        }
        private static void RunTask3()
        {
            Console.WriteLine("=== ЗАВДАННЯ 3: Action, Func, Predicate без LINQ ===");
            List<ServiceOrder> orders = CreateSampleOrders();

            // Фільтрація через Predicate<T> без LINQ (метод List<T>.FindAll)
            Predicate<ServiceOrder> byBudget = o => o.Budget >= 15000m;
            Predicate<ServiceOrder> byText = o => o.ClientTitle.Contains("Tech", StringComparison.OrdinalIgnoreCase);
            Predicate<ServiceOrder> byCombined = o => o.IsUrgent && o.Budget > 10000m;

            Console.WriteLine($"Бюджет >= 15000: {orders.FindAll(byBudget).Count} шт.");
            Console.WriteLine($"Клієнт містить 'Tech': {orders.FindAll(byText).Count} шт.");
            Console.WriteLine($"Термінові > 10000: {orders.FindAll(byCombined).Count} шт.");

            // Перетворення через Func<T, TResult> без LINQ
            List<string> summaries = MapManual(orders, o => $"#{o.Id} {o.ClientTitle}: {o.Budget:F0} грн (з ПДВ: {o.Budget * 1.2m:F0})");
            Console.WriteLine("Перший перетворений рядок: " + summaries[0]);

            // Обхід через Action<T> для накопичення статистики
            decimal urgentTotal = 0m;
            Action<ServiceOrder> accumulateUrgent = o =>
            {
                if (o.IsUrgent) urgentTotal += o.Budget;
            };
            orders.ForEach(accumulateUrgent);
            Console.WriteLine($"Сума термінових замовлень (через Action): {urgentTotal:F2} грн");

            // Конвеєр перетворень List<Func<T, T>>
            var pipeline = new List<Func<decimal, decimal>>
    {
        price => price * 1.20m,    // +20% ПДВ
        price => price - 500m,     // промокод -500 грн
        price => Math.Round(price, 2)
    };
            decimal finalPrice = ApplyPipeline(10000m, pipeline);
            Console.WriteLine($"Результат конвеєра Func<decimal, decimal> для 10000: {finalPrice:F2} грн");
        }

        private static List<TResult> MapManual<T, TResult>(List<T> source, Func<T, TResult> selector)
        {
            var result = new List<TResult>(source.Count);
            foreach (var item in source) result.Add(selector(item));
            return result;
        }

        private static T ApplyPipeline<T>(T initialValue, IEnumerable<Func<T, T>> steps)
        {
            T current = initialValue;
            foreach (var step in steps) current = step(current);
            return current;
        }

        private static List<ServiceOrder> CreateSampleOrders() => new List<ServiceOrder>
{
    new ServiceOrder(1, "NovaTech Systems", 18500m, true, new[] { "C#", "Cloud" }),
    new ServiceOrder(2, "GreenMarket", 7200m, false, new[] { "Web", "SEO" }),
    new ServiceOrder(3, "KyivTech Lab", 24000m, true, new[] { "AI", "C#" }),
    new ServiceOrder(4, "LogisticPro", 12500m, true, new[] { "ERP" }),
    new ServiceOrder(5, "AgroPlus", 9400m, false, new[] { "Mobile" }),
    new ServiceOrder(6, "FinTech Core", 31000m, false, new[] { "Security", "Cloud" }),
    new ServiceOrder(7, "BuildMaster", 15200m, true, new[] { "CRM" }),
    new ServiceOrder(8, "MedService", 6800m, false, new[] { "Support" })
};
        private static void RunTask4()
        {
            Console.WriteLine("=== ЗАВДАННЯ 4: LINQ, агрегація та матеріалізація ===");
            List<ServiceOrder> orders = CreateSampleOrders();

            // 1. Запит на вбудованих лямбдах vs делегати у змінних Func
            var inlineQuery = orders
                .Where(o => o.Budget >= 15000m)
                .Select(o => $"{o.ClientTitle} ({o.Budget:F0})");

            Func<ServiceOrder, bool> highBudgetFilter = o => o.Budget >= 15000m;
            Func<ServiceOrder, string> titleSelector = o => $"{o.ClientTitle} ({o.Budget:F0})";
            var delegateVarQuery = orders.Where(highBudgetFilter).Select(titleSelector);

            Console.WriteLine("LINQ (вбудовані лямбди): " + string.Join(", ", inlineQuery));
            Console.WriteLine("LINQ (через змінні Func): " + string.Join(", ", delegateVarQuery));

            // 2. Впорядкування (OrderByDescending + ThenBy) та SelectMany
            var sorted = orders
                .OrderByDescending(o => o.IsUrgent)
                .ThenBy(o => o.Budget)
                .Select(o => $"#{o.Id}[{(o.IsUrgent ? "Терм." : "Звич.")}:{o.Budget:F0}]");
            Console.WriteLine("Сортування: " + string.Join(" -> ", sorted));

            var uniqueTags = orders.SelectMany(o => o.Tags).Distinct().OrderBy(t => t);
            Console.WriteLine("Усі унікальні теги (SelectMany): " + string.Join(", ", uniqueTags));

            // 3. Агрегація та групування
            int urgentCount = orders.Count(o => o.IsUrgent);
            decimal totalBudget = orders.Sum(o => o.Budget);
            decimal avgBudget = orders.Average(o => o.Budget);
            Console.WriteLine($"Агрегація: термінових = {urgentCount}, сума = {totalBudget:F2}, середнє = {avgBudget:F2}");

            foreach (var group in orders.GroupBy(o => o.IsUrgent))
            {
                Console.WriteLine($"  Група IsUrgent={group.Key}: {group.Count()} замовлень, сума {group.Sum(x => x.Budget):F0} грн");
            }

            // 4. Відкладене виконання vs матеріалізація (.ToList())
            IEnumerable<ServiceOrder> deferredQuery = orders.Where(o => o.Budget > 20000m);
            List<ServiceOrder> materializedList = deferredQuery.ToList(); // фіксує стан зараз

            orders.Add(new ServiceOrder(9, "MegaEnterprise", 50000m, true, new[] { "Enterprise" }));

            Console.WriteLine($"Після додавання 9-го елемента -> відкладений запит бачить: {deferredQuery.Count()} шт., матеріалізований список: {materializedList.Count} шт.");

            Action<ServiceOrder> printMaterialized = o => Console.WriteLine($"  [Матеріалізований]: {o.ClientTitle} — {o.Budget:F0} грн");
            materializedList.ForEach(printMaterialized);
        }
    }

    public static class PricingRules
    {
        public static double ApplyLinearMarkup(double baseValue, double factor) => baseValue * factor;
    }

    public sealed class TaxAdjuster
    {
        private readonly double _taxRate;
        public TaxAdjuster(double taxRate) => _taxRate = taxRate;
        public double ApplyCompoundRate(double baseValue, double factor) => baseValue * factor * (1.0 + _taxRate);
    }

    public sealed class ServiceOrder
    {
        public int Id { get; }
        public string ClientTitle { get; }
        public decimal Budget { get; }
        public bool IsUrgent { get; }
        public string[] Tags { get; }

        public ServiceOrder(int id, string clientTitle, decimal budget, bool isUrgent, string[] tags)
        {
            Id = id;
            ClientTitle = clientTitle;
            Budget = budget;
            IsUrgent = isUrgent;
            Tags = tags;
        }
    }
}