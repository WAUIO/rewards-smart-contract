using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace Voxiberate
{
    public enum RewardState : byte
    {
        Opened,
        Locked,
        Distributed,
        Refunded
    }
    public class RewardsInfo
    {
        public UInt160 Facilitator = UInt160.Zero;
        public UInt160 Token = UInt160.Zero;
        public BigInteger Amount;
        public UInt160[] Citizens;
        public RewardState State;
    }

    [DisplayName("Voxiberate.RewardsContract")]
    [ManifestExtra("Author", "Voxiberate")]
    [ManifestExtra("Description", "Voxiberation rewards contract")]
    [ContractPermission("*", "transfer")]
    public class RewardsContract : SmartContract
    {
        const byte PREFIX_REWARDS = 0x00;
        const byte PREFIX_ADMIN = 0x01;
        const byte PREFIX_CITIZEN_MAP = 0x02;
        const byte PREFIX_CONTRACT_OWNER = 0xFF;

        private static Transaction Tx => (Transaction)Runtime.ScriptContainer;

        [Safe]
        public static UInt160 Owner() => (UInt160)Storage.Get(Storage.CurrentContext, new byte[] { PREFIX_CONTRACT_OWNER });

        [Safe]
        public static UInt160 Admin(UInt160 address) => (UInt160)new StorageMap(Storage.CurrentContext, PREFIX_ADMIN).Get(address);

        public delegate void OnRewardStoredDelegate(string rewardKey, UInt160 facilitator, UInt160 token, BigInteger amount);

        public delegate void OnRewardRefundedDelegate(string rewardKey, UInt160 facilitator, RewardState state);

        public delegate void OnRewardCitizenDelegate(string rewardKey, UInt160 citizen, string type, bool status, BigInteger amount);

        public delegate void OnCitizenSubmittedDelegate(string rewardKey);

        public delegate void OnRewardDistributedDelegate(string rewardKey, Map<string, BigInteger> rewarded);

        public delegate void OnAdminAddedDelegate(UInt160 admin);

        public delegate void OnAdminRemovedDelegate(UInt160 admin);

        public delegate void OnRewardLockedDelegate(string rewardKey);

        [DisplayName("RewardStored")]
        public static event OnRewardStoredDelegate OnRewardStored = default!;

        [DisplayName("RewardRefunded")]
        public static event OnRewardRefundedDelegate OnRewardRefunded = default!;

        [DisplayName("CitizenSubmitted")]
        public static event OnCitizenSubmittedDelegate OnCitizenSubmitted = default!;

        [DisplayName("RewardDistributed")]
        public static event OnRewardDistributedDelegate OnRewardDistributed = default!;

        [DisplayName("AdminAdded")]
        public static event OnAdminAddedDelegate OnAdminAdded = default!;

        [DisplayName("AdminRemoved")]
        public static event OnAdminRemovedDelegate OnAdminRemoved = default!;

        [DisplayName("RewardLocked")]
        public static event OnRewardLockedDelegate OnRewardLocked = default!;

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            var key = new byte[] { PREFIX_CONTRACT_OWNER };
            Storage.Put(Storage.CurrentContext, key, Tx.Sender);
            new StorageMap(Storage.CurrentContext, PREFIX_ADMIN).Put(Tx.Sender, Tx.Sender);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!Runtime.CheckWitness(Owner())) throw new Exception("Only owner can update the contract.");
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            if (!Runtime.CheckWitness(Owner())) throw new Exception("Only owner can destroy the contract.");
            ContractManagement.Destroy();
        }

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            return (bool)Contract.Call(Runtime.CallingScriptHash, "transfer", CallFlags.All, new object[] { from, to, amount, data });
        }

        public static void AddAdmin(UInt160 admin)
        {
            var owner = Owner();
            if (!Runtime.CheckWitness(owner)) throw new Exception("Only owner can add new admin.");
            new StorageMap(Storage.CurrentContext, PREFIX_ADMIN).Put(admin, admin);

            OnAdminAdded(admin);
        }

        public static void RemoveAddmin(UInt160 admin)
        {
            if (!Runtime.CheckWitness(Owner())) throw new Exception("Only owner can remove an admin.");
            new StorageMap(Storage.CurrentContext, PREFIX_ADMIN).Delete(admin);

            OnAdminRemoved(admin);
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, string rewardKey)
        {
            if (from == null && Runtime.CallingScriptHash == GAS.Hash)
            {
                // When NEO balance changes, the contract receives GAS from the platform
                // Ignore this payment for rewards purposes
                return;
            }

            if (from == null) throw new ArgumentNullException(nameof(from));

            if (rewardKey != null)
            {
                StoreRewards(from, amount, rewardKey);
            }
        }

        [DisplayName("_storeRewards")]
        static void StoreRewards(UInt160 facilitator, BigInteger amount, string rewardKey)
        {
            var token = Runtime.CallingScriptHash;
            StorageMap rewardMap = new(Storage.CurrentContext, PREFIX_REWARDS);
            var serializedReward = rewardMap.Get(rewardKey);
            if (serializedReward != null) throw new Exception("Specified rewardKey already exists.");

            var rewardInfo = new RewardsInfo();
            rewardInfo.Facilitator = facilitator;
            rewardInfo.Token = token;
            rewardInfo.Amount = amount;
            rewardInfo.State = RewardState.Opened;

            rewardMap.Put(rewardKey, StdLib.Serialize(rewardInfo));

            OnRewardStored(rewardKey, facilitator, token, amount);
        }

        public static void Refund(string rewardKey)
        {
            if (!Runtime.CheckWitness(Admin(Tx.Sender))) throw new Exception("Only a admin can refund a reward.");

            StorageMap rewardMap = new(Storage.CurrentContext, PREFIX_REWARDS);
            var serializedReward = rewardMap.Get(rewardKey);

            if (serializedReward == null) throw new Exception("Rewards not found.");

            var rewardInfo = (RewardsInfo)StdLib.Deserialize(serializedReward);

            if (rewardInfo.State == RewardState.Refunded) throw new Exception("Rewards already refunded.");

            if (rewardInfo.State == RewardState.Distributed) throw new Exception("Rewards already distributed.");

            var isRefunded = (bool)Contract.Call(rewardInfo.Token, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, rewardInfo.Facilitator, rewardInfo.Amount, new byte[] { } });

            if (!isRefunded) throw new Exception("Unable to refund rewards.");

            rewardInfo.State = RewardState.Refunded;
            rewardMap.Put(rewardKey, StdLib.Serialize(rewardInfo));

            OnRewardRefunded(rewardKey, rewardInfo.Facilitator, rewardInfo.State);
        }

        public static void LockRewards(string rewardKey)
        {
            if (!Runtime.CheckWitness(Admin(Tx.Sender))) throw new Exception("Only an admin can lock a reward.");

            StorageMap rewardMap = new(Storage.CurrentContext, PREFIX_REWARDS);
            var serializedReward = rewardMap.Get(rewardKey);

            if (serializedReward == null) throw new Exception("Invalid reward id.");
            var rewardInfo = (RewardsInfo)StdLib.Deserialize(serializedReward);
            rewardInfo.State = RewardState.Locked;
            rewardMap.Put(rewardKey, StdLib.Serialize(rewardInfo));

            OnRewardLocked(rewardKey);
        }


        public static void SubmitCitizens(string rewardKey, UInt160[] citizens)
        {
            if (!Runtime.CheckWitness(Admin(Tx.Sender))) throw new Exception("Only an admin can submit citizens.");

            if (citizens == null || citizens.Length == 0) throw new Exception("The parameter citizens cannot be null or empty.");

            StorageMap rewardMap = new(Storage.CurrentContext, PREFIX_REWARDS);
            var serializedReward = rewardMap.Get(rewardKey);

            if (serializedReward == null) throw new Exception("Invalid reward id.");

            var rewardInfo = (RewardsInfo)StdLib.Deserialize(serializedReward);

            if (rewardInfo.State != RewardState.Opened) throw new Exception("Cannot add more citizens");

            StorageMap citizenMap = new(Storage.CurrentContext, PREFIX_CITIZEN_MAP);

            foreach (var c in citizens)
            {
                var cHash = new List<object>();
                cHash.Add(c);
                cHash.Add(rewardKey);
                var ck = CryptoLib.Sha256(StdLib.Serialize(cHash));
                citizenMap.Put(ck, 1);
            }

            OnCitizenSubmitted(rewardKey);
        }

        public static void DistributeRewards(string rewardKey, UInt160[] top5citizens, UInt160[] fullParticitationCitizens)
        {
            if (!Runtime.CheckWitness(Admin(Tx.Sender))) throw new Exception("Only an admin can distribute rewards");

            StorageMap rewardMap = new(Storage.CurrentContext, PREFIX_REWARDS);
            var serializedReward = rewardMap.Get(rewardKey);

            if (serializedReward == null) throw new Exception("Invalid reward key.");

            var rewardInfo = (RewardsInfo)StdLib.Deserialize(serializedReward);

            StorageMap citizenMap = new(Storage.CurrentContext, PREFIX_CITIZEN_MAP);

            Map<string, BigInteger> rewardedMap = new();

            foreach (var c5 in top5citizens)
            {
                var cHash = new List<object>();
                cHash.Add(c5);
                cHash.Add(rewardKey);
                var ck = CryptoLib.Sha256(StdLib.Serialize(cHash));

                var citizenStore = citizenMap.Get(ck);
                if (citizenStore != null)
                {
                    var reward = (rewardInfo.Amount * 10) / 100;
                    var isTransfered = (bool)Contract.Call(rewardInfo.Token, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, c5, reward, new byte[] { } });
                    if (isTransfered) rewardedMap[c5] = reward;
                }

            }

            var fullParticipationRewardPart = (rewardInfo.Amount * 50) / 100;
            foreach (var cf in fullParticitationCitizens)
            {
                var cHash = new List<object>();
                cHash.Add(cf);
                cHash.Add(rewardKey);
                var ck = CryptoLib.Sha256(StdLib.Serialize(cHash));

                var citizenStore = citizenMap.Get(ck);
                if (citizenStore != null)
                {
                    var reward = fullParticipationRewardPart / fullParticitationCitizens.Length;
                    var isTransfered = (bool)Contract.Call(rewardInfo.Token, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, cf, reward, new byte[] { } });
                    if (isTransfered) rewardedMap[cf] = reward;
                }

            }

            rewardInfo.State = RewardState.Distributed;
            rewardMap.Put(rewardKey, StdLib.Serialize(rewardInfo));

            OnRewardDistributed(rewardKey, rewardedMap);

        }
    }
}
