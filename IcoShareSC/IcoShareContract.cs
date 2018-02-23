using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace IcoShareSC
{
    public class IcoShareContract : SmartContract
    {
        #region Private fields
        //private static byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash();
        private static readonly byte[] NeoAssetId = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };

        public static readonly char POSTFIX_STATUS = 'A';
        public static readonly char POSTFIX_STARTDATE = 'B';
        public static readonly char POSTFIX_ENDDATE = 'C';
        public static readonly char POSTFIX_BUNDLE = 'D';
        public static readonly char POSTFIX_MINCONT = 'E';
        public static readonly char POSTFIX_MAXCONT = 'F';
        public static readonly char POSTFIX_CURRENTCONT = 'G';
        public static readonly char POSTFIX_CONTRIBUTORS = 'H';
        public static readonly char POSTFIX_CONTRIBUTEDSHARES = 'I';
        public static readonly char POSTFIX_TOKENHASH = 'J';

        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string FUNDED = "FUNDED";
        public static readonly string NOTFUNDED = "NOTFUNDED";

        private const int IcoShareIdLenght = 36;
        private const int SenderAddresLenght = 34;
        #endregion

        #region Helper 
        private static BigInteger Now()
        {
            uint now = Blockchain.GetHeader(Blockchain.GetHeight()).Timestamp;
            now += 15; //TODO : ???
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
        private static bool IsOwner()
        {
            return true; //TODO : Fix this when release
            //return Runtime.CheckWitness(Owner);
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
                if (
                    //IsEquel(output.ScriptHash, GetReceiver()) && //TODO : Commented for now, this produces false with neo-debugger
                    IsEquel(output.AssetId, NeoAssetId))
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
            Runtime.Notify(k, value);
            return value;
        }
        private static byte[] GetFromStorage(byte[] storageKey, char postfix)
        {
            string k = storageKey.AsString() + postfix;
            var value = Storage.Get(Storage.CurrentContext, k.AsByteArray());
            Runtime.Notify(k, value);
            return value;
        }
        private static byte[] GetFromStorage(byte[] storageKey)
        {
            var value = Storage.Get(Storage.CurrentContext, storageKey);
            Runtime.Notify(storageKey, value);
            return value;
        }
        private static byte[] GetFromStorage(string storageKey)
        {
            var value = Storage.Get(Storage.CurrentContext, storageKey);
            Runtime.Notify(storageKey, value);
            return value;
        }
        private static byte[][] GetListFromStorage(string storageKey, char postfix, int listItemSize)
        {
            var key = storageKey + postfix;
            var value = GetListFromStorage(key, listItemSize);
            Runtime.Notify(key, value);
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

        private static void PutOnStorage(string storageKey, BigInteger value)
        {
            Runtime.Notify(storageKey, value + 0);
            Storage.Put(Storage.CurrentContext, storageKey, value);
        }
        private static void PutOnStorage(byte[] storageKey, BigInteger value)
        {
            Runtime.Notify(storageKey, value + 0);
            Storage.Put(Storage.CurrentContext, storageKey, value);
        }
        private static void PutOnStorage(byte[] storageKey, byte[] value)
        {
            Runtime.Notify(storageKey, value);
            Storage.Put(Storage.CurrentContext, storageKey, value);
        }
        private static void PutOnStorage(string storageKey, byte[] value)
        {
            Runtime.Notify(storageKey, value);
            Storage.Put(Storage.CurrentContext, storageKey, value);
        }
        private static void PutOnStorage(byte[] storageKey, char postfix, BigInteger value)
        {
            string k = string.Concat(storageKey.AsString(), postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value + 0);
        }
        private static void PutOnStorage(byte[] storageKey, char postfix, string value)
        {
            string k = string.Concat(storageKey.AsString(), postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value + 0);
        }
        private static void PutOnStorage(string storageKey, char postfix, BigInteger value)
        {
            string k = string.Concat(storageKey, postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value + 0);
        }
        private static void PutOnStorage(string storageKey, char postfix, byte[] value)
        {
            string k = string.Concat(storageKey, postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value);
        }
        private static void PutOnStorage(string storageKey, char postfix, string value)
        {
            string k = string.Concat(storageKey, postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value);
        }
        private static void PutOnStorage(byte[] storageKey, char postfix, byte[] value)
        {
            string k = string.Concat(storageKey.AsString(), postfix);
            Storage.Put(Storage.CurrentContext, k.AsByteArray(), value);
            Runtime.Notify(k, value);
        }
        /// <summary>
        /// Stores one to many relation. storageKey+postfix+value is unique
        /// </summary>
        /// <param name="storageKey"></param>
        /// <param name="postfix"></param>
        /// <param name="value"></param>
        private static void PutItemOnStorageList(string storageKey, char postfix, byte[] value, int listItemSize)
        {
            var list = GetListFromStorage(storageKey, postfix, listItemSize);

            //Check if value is already exist
            if (list != null)
                for (int i = 0; i < list.Length; i++)
                    if (IsEquel(list[i], value)) return;

            //Update list string
            var item = GetFromStorage(storageKey, postfix).AsString() ?? "";

            if (item != null && item != "")
                item = string.Concat(item, "_");

            item = string.Concat(item, value.AsString());

            PutOnStorage(storageKey, postfix, item.AsByteArray());
        }

        private static byte[] IntToBytes(BigInteger value)
        {
            byte[] buffer = value.ToByteArray();
            return buffer;
        }
        private static BigInteger BytesToInt(byte[] array)
        {
            return array.AsBigInteger() + 0;
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
        private static string MultiKey(params string[] keys)
        {
            string temp = keys[0];

            for (int i = 1; i < keys.Length; i++)
            {
                temp = string.Concat(temp, "_", keys[i]);
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
            ulong contribute_value = GetContributeValue();
            if (contribute_value > 0 && sender.Length != 0)
            {
                OnRefund(sender, contribute_value);
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

            if (Runtime.Trigger == TriggerType.Verification)
            {
                //Get invoker address
                //Check if invoker can withdraw that amount 
                //Allow/Reject withdrawal
            }

            if (operation == "startNewIcoShare") return StartNewIcoShare(
                (string)args[0], (string)args[1],
                (BigInteger)args[2], (BigInteger)args[3],
                (BigInteger)args[4], (BigInteger)args[5], (BigInteger)args[6]);
            if (operation == "sendContribution") return SendContribution((string)args[0]);

            //Not supported opetation, refund 
            RefundContributedValue();
            return false;
        }

        //START NEW ICO
        public static bool StartNewIcoShare(
            string icoShareId, string tokenScriptHash,
            BigInteger startTime, BigInteger endTime,
            BigInteger contributionBundle, BigInteger minContribution, BigInteger maxContribution)
        {
            //Check parameters
            if (icoShareId.Length != IcoShareIdLenght || endTime < Now() || endTime < startTime) return false;

            //Check if id already used
            var existingId = GetFromStorage(icoShareId, POSTFIX_STATUS);
            if (existingId != null) return false;

            //Set Ico Share Info
            PutOnStorage(icoShareId, POSTFIX_STATUS, ACTIVE);
            PutOnStorage(icoShareId, POSTFIX_TOKENHASH, tokenScriptHash.AsByteArray());
            PutOnStorage(icoShareId, POSTFIX_STARTDATE, startTime);
            PutOnStorage(icoShareId, POSTFIX_ENDDATE, endTime);
            PutOnStorage(icoShareId, POSTFIX_BUNDLE, contributionBundle);
            PutOnStorage(icoShareId, POSTFIX_MINCONT, minContribution);
            PutOnStorage(icoShareId, POSTFIX_MAXCONT, maxContribution);
            PutOnStorage(icoShareId, POSTFIX_CURRENTCONT, 0);

            return true;
        }

        //SEND CONTRIBUTION
        public static bool SendContribution(string icoShareId)
        {
            //Sender's address
            byte[] sender = GetSender();

            //Contribute asset is not neo
            if (sender.Length == 0) return false;

            //Get contribution value
            BigInteger contributeValue = GetContributeValue();
            if (contributeValue == 0) return false;
            Runtime.Notify("Contributed value", contributeValue);

            ////Check if IcoShare funded
            //var isIcoShareFunded = GetFromStorage(icoShareId, POSTFIX_STATUS).AsString();
            //if (isIcoShareFunded != ACTIVE)
            //{
            //    OnRefund(sender, contributeValue);
            //    return false;
            //}

            ////Check enddate
            //BigInteger endDate = GetFromStorage(icoShareId, POSTFIX_ENDDATE).AsBigInteger();
            //if (endDate < Now())
            //{
            //    OnRefund(sender, contributeValue);
            //    return false;
            //}

            //IcoShare details 
            BigInteger icoShareCurrentAmount = GetFromStorage(icoShareId, POSTFIX_CURRENTCONT).AsBigInteger();  Runtime.Notify("icoShareCurrentAmount", icoShareCurrentAmount +0);
            BigInteger icoShareBundle = GetFromStorage(icoShareId, POSTFIX_BUNDLE).AsBigInteger();                  Runtime.Notify("icoShareBundle", icoShareBundle +0);
            BigInteger icoShareMax = GetFromStorage(icoShareId, POSTFIX_MAXCONT).AsBigInteger();                    Runtime.Notify("icoShareMax", icoShareMax + 0);
            BigInteger sendersCurrentCont = GetFromStorage(MultiKey(icoShareId, sender.AsString())).AsBigInteger(); Runtime.Notify("sendersCurrentCont", sendersCurrentCont + 0);

            //Decide to the contribution
            BigInteger contribution = 0;

            //Check maximum contribution for sender
            if (sendersCurrentCont + contributeValue > icoShareMax)
            {
                Runtime.Notify(sendersCurrentCont, contributeValue, sendersCurrentCont + contributeValue, icoShareMax);
                Runtime.Notify("User reached to his/her maximum, refund more than icoShareMax");

                var calc = icoShareMax - sendersCurrentCont;

                BigInteger refundAmount = contributeValue - calc;
                OnRefund(sender, refundAmount);

                contribution = calc;
            }
            else
            {
                contribution = contributeValue;
            }

            //Check if IcoShare current amount reaches full
            if (icoShareCurrentAmount + contribution > icoShareBundle)
            {
                //User reached to icoShare bundle amount, refund more than icoShareBundle

                var calc = icoShareBundle - icoShareCurrentAmount;

                //Refund 
                BigInteger refund = contribution - calc;
                OnRefund(sender, refund);

                contribution = calc;

                if (contribution == 0) return false;
            }
            Runtime.Notify("Contribution", contribution);

            //Add/Update user's current contribution
            if (sendersCurrentCont > 0)
            {
                //Update sender's contribution
                PutOnStorage(MultiKey(icoShareId, sender), (sendersCurrentCont + contribution).AsByteArray());
            }
            else
            {
                //Add new contribution amount to sender
                PutOnStorage(MultiKey(icoShareId, sender), contribution.AsByteArray());

                //Add to icoshare's contributors
                PutItemOnStorageList(icoShareId.AsString(), POSTFIX_CONTRIBUTORS, sender, SenderAddresLenght);

                //Add to contributor's icoShare list
                PutItemOnStorageList(sender.AsString(), POSTFIX_CONTRIBUTEDSHARES, icoShareId, IcoShareIdLenght);
            }

            //Update IcoShare's current value
            BigInteger icoShareNewAmount = icoShareCurrentAmount + contribution;
            PutOnStorage(icoShareId, POSTFIX_CURRENTCONT, icoShareNewAmount.AsByteArray());
            Runtime.Notify("IcoShare's new amount", icoShareNewAmount);

            //Check if IcoShare completed 
            if (icoShareNewAmount == icoShareBundle)
            {
                PutOnStorage(icoShareId, POSTFIX_STATUS, FUNDED);
                OnFunded(icoShareId);
            }

            return true;
        }

        //GET CURRENT CONTRIBUTION
        public static BigInteger GetCurrentContribution(byte[] icoShareId)
        {
            return GetFromStorage(icoShareId, POSTFIX_CURRENTCONT).AsBigInteger();
        }

        //REFUND, ICOSHARE IS UNSUCCESFULL
        //Invoked by owner when time limit reached
        public static bool RefundUnsuccesfullIcoShare(byte[] icoShareId)
        {
            if (!IsOwner()) return false;

            //Get contibutor list 
            var key = string.Concat(icoShareId.AsString(), POSTFIX_CONTRIBUTORS);
            var contributors = GetListFromStorage(key, SenderAddresLenght);

            //Refund every contribution
            for (int i = 0; i < contributors.Length; i++)
            {
                var amount = GetFromStorage(MultiKey(icoShareId, contributors[i])).AsBigInteger();
                OnRefund(contributors[i], amount);
            }

            //Cancel IcoShare
            PutOnStorage(icoShareId, POSTFIX_STATUS, NOTFUNDED);

            return true;
        }

        public bool DistributeNep5Tokens(byte[] icoShareId)
        {
            if (!IsOwner()) return false;
            if (GetFromStorage(icoShareId, POSTFIX_STATUS).AsString() != FUNDED)
            {
                //TODO : Refun Nep5Token 
                return false;
            }

            //Get icoShare details 

            //Get contributed Nep5Token details 

            //Distribute to contributers' addresses

            return true;
        }

        public void GetToken()
        {
            //Get token script hash, 

            //Get token amount 

            //Find ico share by token script hash

            //Distribute tokens 
        }
    }
}
