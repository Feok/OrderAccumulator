using QuickFix;
using QuickFix.Fields;

namespace Executor
{
    public class Acceptor : MessageCracker, IApplication
    {
        private static readonly decimal EXPOSED_LIMIT = 100000000;

        private static decimal PETR4_EXPOSED = 0;
        private static decimal VALE3_EXPOSED = 0;
        private static decimal VIIA4_EXPOSED = 0;
        Dictionary<string, decimal> exposedValues = new Dictionary<string, decimal>()
        {
            { "PETR4", PETR4_EXPOSED },
            { "VALE3", VALE3_EXPOSED },
            { "VIIA4", VIIA4_EXPOSED }
        };

        int orderID = 0;
        int execID = 0;


        private string GenOrderID() { return (++orderID).ToString(); }
        private string GenExecID() { return (++execID).ToString(); }

        #region QuickFix.Application Methods

        public void FromApp(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }

        public void ToApp(Message message, SessionID sessionID)
        {
        }

        public void FromAdmin(Message message, SessionID sessionID) { }
        public void OnCreate(SessionID sessionID) { }
        public void OnLogout(SessionID sessionID) { }
        public void OnLogon(SessionID sessionID) { }
        public void ToAdmin(Message message, SessionID sessionID) { }
        #endregion

        #region MessageCracker overloads

        public void OnMessage(QuickFix.FIX44.NewOrderSingle n, SessionID s)
        {
            Symbol symbol = n.Symbol;
            Side side = n.Side;
            OrdType ordType = n.OrdType;
            OrderQty orderQty = n.OrderQty;
            Price price = new Price();
            ClOrdID clOrdID = n.ClOrdID;

            switch (ordType.getValue())
            {
                case OrdType.LIMIT:
                    price = n.Price;
                    if (price.Obj == 0)
                        throw new IncorrectTagValue(price.Tag);
                    break;
                case OrdType.MARKET: break;
                default: throw new IncorrectTagValue(ordType.Tag);
            }

            QuickFix.FIX44.ExecutionReport exReport;
            decimal priceFromSide = side.getValue().Equals(Side.BUY)
                ? price.getValue() * orderQty.getValue()
                : price.getValue() * orderQty.getValue() * -1;
            if (HasExposedValue(symbol.getValue(), priceFromSide))
            {
                exReport = new QuickFix.FIX44.ExecutionReport(
                    new OrderID(GenOrderID()),
                    new ExecID(GenExecID()),
                    new ExecType(ExecType.NEW),
                    new OrdStatus(OrdStatus.NEW),
                    symbol,
                    side,
                    new LeavesQty(0),
                    new CumQty(orderQty.getValue()),
                    new AvgPx(price.getValue()));

                exReport.Set(clOrdID);
                exReport.Set(symbol);
                exReport.Set(orderQty);
                exReport.Set(new LastQty(orderQty.getValue()));
                exReport.Set(new LastPx(price.getValue()));

                if (n.IsSetAccount())
                    exReport.SetField(n.Account);
                Console.WriteLine("NEW");
            }
            else
            {
                exReport = new QuickFix.FIX44.ExecutionReport(
                    new OrderID(GenOrderID()),
                    new ExecID(GenExecID()),
                    new ExecType(ExecType.REJECTED),
                    new OrdStatus(OrdStatus.REJECTED),
                    symbol,
                    side,
                    new LeavesQty(0),
                    new CumQty(orderQty.getValue()),
                    new AvgPx(price.getValue()));
                Console.WriteLine("REJECT");
            }

            try
            {
                Session.SendToTarget(exReport, s);
            }
            catch (SessionNotFound ex)
            {
                Console.WriteLine("==session not found exception!==");
                Console.WriteLine(ex.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private bool HasExposedValue(string symbol, decimal priceFromSide)
        {
            exposedValues.TryGetValue(symbol, out var exposedValue);
            if (Math.Abs(exposedValue + priceFromSide) >= EXPOSED_LIMIT)
            {
                Console.WriteLine($"{symbol}={priceFromSide}+{exposedValue}");
                Console.WriteLine($"{symbol}_EXPOSED={exposedValues[symbol]}");
                return false;
            }
            else
            {
                Console.WriteLine($"{symbol}={priceFromSide}+{exposedValue}");
                exposedValues[symbol] = exposedValue += priceFromSide;
                Console.WriteLine($"{symbol}_EXPOSED={exposedValues[symbol]}");
                return true;
            }
        }
            #endregion //MessageCracker overloads
        }
}