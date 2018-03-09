using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace IcoShareSC
{
    [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
    [SuppressMessage("ReSharper", "SuggestVarOrType_BuiltInTypes")]
    [SuppressMessage("ReSharper", "SuggestVarOrType_SimpleTypes")]
    public class IcoShareContract : SmartContract
    {
        #region Private fields
        //private static byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash();
        private static readonly byte[] NeoAssetId = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private const ulong NeoDecimals = 100000000;

        public static readonly char PostfixStatus = 'A';
        public static readonly char PostfixStartdate = 'B';
        public static readonly char PostfixEnddate = 'C';
        public static readonly char PostfixBundle = 'D';
        public static readonly char PostfixMincont = 'E';
        public static readonly char PostfixMaxcont = 'F';
        public static readonly char PostfixCurrentcont = 'G';
        public static readonly char PostfixContributors = 'H';
        public static readonly char PostfixContributedshares = 'I';
        public static readonly char PostfixTokenhash = 'J';

        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string FUNDED = "FUNDED";
        public static readonly string NOTFUNDED = "NOTFUNDED";
        public static readonly string DISTRUBUTED = "DISTRUBUTED";

        private const int IcoShareIdLenght = 36;
        private const int SenderAddresLenght = 34;
        #endregion

        #region Helper 
        private static BigInteger Now()
        {
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            now += 15; 
            return now;
        }
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();

            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId.AsString() == NeoAssetId.AsString())
                    return output.ScriptHash;
            }
            return new byte[0];
        }
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (IsEquel(output.ScriptHash, GetReceiver()) && IsEquel(output.AssetId, NeoAssetId))
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }

        //Storage related
        private static byte[] GetFromStorage(string storageKey, char postfix)
        {
            string k = storageKey + postfix;
            var value = Storage.Get(Storage.CurrentContext, k.AsByteArray());
            //Runtime.Notify(k, value);
            return value;
        }
        private static byte[] GetFromStorage(string storageKey)
        {
            var value = Storage.Get(Storage.CurrentContext, storageKey);
            //Runtime.Notify(storageKey, value);
            return value;
        }
        private static byte[][] GetListFromStorage(string storageKey, int listItemSize)
        {
            byte[] storageItem = GetFromStorage(storageKey);
            if (storageItem == null || storageItem.Length == 0) return null;

            var list = storageItem.AsString();

            listItemSize = listItemSize + 1; //for seperator
            var len = (list.Length + 1) / listItemSize;

            byte[][] liste = new byte[len][];

            for (int i = 0; i < len; i++)
            {
                liste[i] = list.Substring(i * listItemSize, listItemSize - 1).AsByteArray();
            }

            return liste;
        }

        private static void PutOnStorage(string storageKey, byte[] value)
        {
            //Runtime.Notify(storageKey, value);
            Storage.Put(Storage.CurrentContext, storageKey, value);
        }
        private static void PutOnStorage(string storageKey, char postfix, BigInteger value)
        {
            string k = string.Concat(storageKey, postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            //Runtime.Notify(k, value + 0);
        }
        private static void PutOnStorage(string storageKey, char postfix, string value)
        {
            string k = string.Concat(storageKey, postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            //Runtime.Notify(k, value);
        }
        private static void PutItemOnStorageList(string storageKey, char postfix, byte[] value, int listItemSize)
        {
            var listKey = storageKey + postfix;
            var list = GetListFromStorage(listKey, listItemSize);

            //Check if value is already exist
            if (list != null)
                for (int i = 0; i < list.Length; i++)
                    if (IsEquel(list[i], value)) return;

            //Update list string
            var item = GetFromStorage(storageKey, postfix).AsString() ?? "";

            if (item != null && item != "")
                item = string.Concat(item, "_");

            item = string.Concat(item, value.AsString());

            PutOnStorage(storageKey, postfix, item);
        }
        
        private static string MultiKey(params byte[][] keys)
        {
            string temp = keys[0].AsString();

            for (int i = 1; i < keys.Length; i++)
            {
                temp = string.Concat(temp, "_", keys[i].AsString());
            }

            return temp;
        }
        private static bool IsEquel(byte[] value1, byte[] value2)
        {
            return value1.AsString() == value2.AsString();
        }

        private static void RefundContributedValue()
        {
            byte[] sender = GetSender();
            ulong contributedValue = GetContributeValue();
            if (contributedValue > 0 && sender.Length != 0)
            {
                OnRefund(sender, contributedValue);
            }
        }
        #endregion

        #region Events

        [DisplayName("funded")]
        public static event Action<byte[]> Funded;
        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> Refund;

        private static void OnRefund(byte[] address, BigInteger amount)
        {
            Refund(address, amount);
        }
        private static void OnFunded(byte[] icoShareId)
        {
            Funded(icoShareId);
        }
        #endregion

        public static Object Main(string operation, params object[] args)
        {
            Runtime.Notify("Operation", operation);

            if (operation == "startNewIcoShare") return StartNewIcoShare(args);
            if (operation == "sendContribution") return SendContribution(args);
            if (operation == "getCurrentContribution") return GetCurrentContribution((string)args[0]);
            
            //Not supported opetation, refund 
            RefundContributedValue();
            return false;
        }

        //START NEW ICO
        public static bool StartNewIcoShare(object[] args)
        {
            string icoShareId  = (string)args[0];
            string tokenScriptHash = (string) args[1];
            BigInteger startTime = (BigInteger)args[2];
            BigInteger endTime = (BigInteger) args[3];
            BigInteger contributionBundle = (BigInteger) args[4];
            BigInteger minContribution = (BigInteger) args[5];
            BigInteger maxContribution = (BigInteger)args[6];
            Runtime.Notify(maxContribution +0);

            //Check parameters
            if (icoShareId.Length != IcoShareIdLenght ) return false;
            if (endTime < Now()) return false;
            if (endTime < startTime) return false;

            //Check if id already used
            var existingId = GetFromStorage(icoShareId, PostfixStatus);
            if (existingId != null) return false;

            //Set Ico Share Info
            PutOnStorage(icoShareId, PostfixStatus, ACTIVE);
            PutOnStorage(icoShareId, PostfixTokenhash, tokenScriptHash);
            PutOnStorage(icoShareId, PostfixStartdate, startTime+0);
            PutOnStorage(icoShareId, PostfixEnddate, endTime+0);
            PutOnStorage(icoShareId, PostfixBundle, contributionBundle+0);
            PutOnStorage(icoShareId, PostfixMincont, minContribution+0);
            PutOnStorage(icoShareId, PostfixMaxcont, maxContribution+0);
            PutOnStorage(icoShareId, PostfixCurrentcont, 0);

            return true;
        }

        //SEND CONTRIBUTION
        public static bool SendContribution(object[] args)
        {
            string icoShareId = (string) args[0];
            Runtime.Notify((string)args[0]);

            //Sender's address
            byte[] sender = GetSender();

            //Contribute asset is not neo
            if (sender.Length == 0) return false;

            //Get contribution value 
            BigInteger contributeValue = GetContributeValue() / NeoDecimals;
            if (contributeValue == 0) return false;
            Runtime.Notify("Contributed value", contributeValue);

            //Check if IcoShare funded
            var isIcoShareFunded = GetFromStorage(icoShareId, PostfixStatus).AsString();
            if (isIcoShareFunded != ACTIVE)
            {
                OnRefund(sender, contributeValue);
                return false;
            }

            //Check enddate
            BigInteger endDate = GetFromStorage(icoShareId, PostfixEnddate).AsBigInteger();
            if (endDate < Now())
            {
                OnRefund(sender, contributeValue);
                return false;
            }

            //IcoShare details 
            BigInteger icoShareCurrentAmount = GetFromStorage(icoShareId, PostfixCurrentcont).AsBigInteger();
            Runtime.Notify("icoShareCurrentAmount", icoShareCurrentAmount +0);

            BigInteger icoShareBundle = GetFromStorage(icoShareId, PostfixBundle).AsBigInteger();
            Runtime.Notify("icoShareBundle", icoShareBundle +0);

            BigInteger icoShareMax = GetFromStorage(icoShareId, PostfixMaxcont).AsBigInteger();
            Runtime.Notify("icoShareMax", icoShareMax + 0);

            BigInteger icoShareMin = GetFromStorage(icoShareId, PostfixMincont).AsBigInteger();
            Runtime.Notify("icoShareMax", icoShareMin + 0);

            BigInteger sendersCurrentCont = GetFromStorage(MultiKey(icoShareId.AsByteArray(), sender)).AsBigInteger();
            Runtime.Notify("sendersCurrentCont", sendersCurrentCont + 0);

            //Decide to the contribution
            BigInteger contribution = 0;

            //Check maximum contribution for sender
            if (contributeValue < icoShareMin)
            {
                OnRefund(sender, contributeValue);
                return false;
            }

            if (sendersCurrentCont + contributeValue > icoShareMax)
            {
                Runtime.Notify(sendersCurrentCont, contributeValue, sendersCurrentCont + contributeValue, icoShareMax);
                Runtime.Notify("User reached to his/her maximum, refund more than icoShareMax");

                var calc = icoShareMax - sendersCurrentCont;

                BigInteger refundAmount = contributeValue - calc;
                OnRefund(sender, refundAmount);

                contribution = calc;
            }
            else contribution = contributeValue;

            //Check if IcoShare current amount reaches full
            if (icoShareCurrentAmount + contribution > icoShareBundle)
            {
                //User reached to icoShare bundle amount, refund more than icoShareBundle
                var available = icoShareBundle - icoShareCurrentAmount;

                //Refund 
                BigInteger refund = contribution - available;

                OnRefund(sender, refund); //Refund the amount more than avilable
                contribution = available; //Override contribution amount 

                if (contribution == 0) return false;
            }
            Runtime.Notify("Contribution", contribution);

            //Add/Update user's current contribution
            if (sendersCurrentCont > 0)
            {
                //Update sender's contribution
                PutOnStorage(MultiKey(icoShareId.AsByteArray(), sender), (sendersCurrentCont + contribution).AsByteArray());
            }
            else
            {
                //Add new contribution amount to sender
                PutOnStorage(MultiKey(icoShareId.AsByteArray(), sender), contribution.AsByteArray());

                //Add to icoshare's contributors
                PutItemOnStorageList(icoShareId.AsByteArray().AsString(), PostfixContributors, sender, SenderAddresLenght);

                //Add to contributor's icoShare list
                PutItemOnStorageList(sender.AsString(), PostfixContributedshares, icoShareId.AsByteArray(), IcoShareIdLenght);
            }

            //Update IcoShare's current value
            BigInteger icoShareNewAmount = icoShareCurrentAmount + contribution;
            PutOnStorage(icoShareId, PostfixCurrentcont, icoShareNewAmount);
            Runtime.Notify("IcoShare's new amount", icoShareNewAmount);

            //Check if IcoShare completed 
            if (icoShareNewAmount == icoShareBundle)
            {
                PutOnStorage(icoShareId, PostfixStatus, FUNDED);
                OnFunded(icoShareId.AsByteArray());
            }

            return true;
        }

        //GET CURRENT CONTRIBUTION
        public static BigInteger GetCurrentContribution(string icoShareId)
        {
            return GetFromStorage(icoShareId, PostfixCurrentcont).AsBigInteger();
        }

        //REFUND, ICOSHARE IS UNSUCCESFULL
        public static bool RefundUnsuccesfullIcoShare(string icoShareId)
        {
            //Check end date
            BigInteger endDate = GetFromStorage(icoShareId, PostfixEnddate).AsBigInteger(); Runtime.Notify("EndDate", endDate);
            var status = GetFromStorage(icoShareId, PostfixStatus).AsString(); Runtime.Notify("Status", status);

            //Refund option is not active. 
            if (status == FUNDED) return false; //Already Funded
            if (endDate > Now() && status == ACTIVE) return false; //Still active

            //REFUND
            //Get contibutor list 
            var key = string.Concat(icoShareId, PostfixContributors);
            var contributors = GetListFromStorage(key, SenderAddresLenght); Runtime.Notify("Contributors", contributors);

            //Refund every contribution
            for (int i = 0; i < contributors.Length; i++)
            {
                var amount = GetFromStorage(MultiKey(icoShareId.AsByteArray(), contributors[i])).AsBigInteger();
                OnRefund(contributors[i], amount);
            }

            //Cancel IcoShare
            PutOnStorage(icoShareId, PostfixStatus, NOTFUNDED);

            return true;
        }
        
    }
}
