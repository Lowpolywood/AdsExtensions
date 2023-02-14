#if APPLOVIN
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdsExtensions.Providers 
{
    public class MaxSdkProvider
    {
        string maxSdkKey;
        string interstitialAdUnitId;
        string rewardedAdUnitId;
        string rewardedInterstitialAdUnitId;
        string bannerAdUnitId;
        string mRecAdUnitId;

        public Color BannerColor;

        private bool isBannerShowing;
        private bool isMRecShowing;

        public Action OnInitialize;
        public Action<AdType, string, string> OnError;
        public Action<AdType, string> OnLoad;
        public Action<AdType, string> OnShow;
        public Action<AdType, string> OnEarnReward;
        public Action<AdType, string> OnClose;
        public Action<AdType, string, Revenue> OnPaid;

        public bool IsInitialized { get; private set; }

        public enum AdType
        {
            Banner,
            Interstitial,
            Rewarded,
            RewardedInterstitial,
            MRec
        }

        public bool IsReady(AdType type)
        {
            if (!initializedAdTypes.Contains(type))
                return false;

            switch (type)
            {
                case AdType.Interstitial:
                    return MaxSdk.IsInterstitialReady(interstitialAdUnitId);
                case AdType.RewardedInterstitial:
                    return MaxSdk.IsRewardedInterstitialAdReady(rewardedInterstitialAdUnitId);
                case AdType.Rewarded:
                    return MaxSdk.IsRewardedAdReady(rewardedAdUnitId);

                default:
                    return true;
            }
        }

        List<AdType> initializedAdTypes;
        List<AdType> requestingAdTypes;

        public void Initialize(string sdkKey, bool consent, string bannerId = null, string interstitialId = null, string rewardedId = null, string rewardedInterstitialId = null, string mRecId = null)
        {
            initializedAdTypes = new List<AdType>();
            requestingAdTypes = new List<AdType>();

            maxSdkKey = sdkKey;
            bannerAdUnitId = bannerId;
            interstitialAdUnitId = interstitialId;
            rewardedAdUnitId = rewardedId;
            rewardedInterstitialAdUnitId = rewardedInterstitialId;
            mRecAdUnitId = mRecId;

            MaxSdk.SetHasUserConsent(consent);

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");

                if (!string.IsNullOrEmpty(bannerId))
                {
                    MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adunit, info) => OnAdLoaded(AdType.Banner, adunit);
                    MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adunit, error) => OnAdFailedToLoad(AdType.Banner, adunit, error);
                    MaxSdkCallbacks.Banner.OnAdClickedEvent += (adunit, info) => OnAdClicked(AdType.Banner, adunit);
                    MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adunit, info) => OnAdPaid(AdType.Banner, adunit, info);

                    initializedAdTypes.Add(AdType.Banner);
                }

                if (!string.IsNullOrEmpty(interstitialId))
                {
                    MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adunit, info) => OnAdLoaded(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adunit, error) => OnAdFailedToLoad(AdType.Interstitial, adunit, error);
                    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adunit, error, info) => OnAdFailedToShow(AdType.Interstitial, adunit, error);
                    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adunit, info) => OnAdClose(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adunit, info) => OnAdPaid(AdType.Interstitial, adunit, info);
                    MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adunit, info) => OnAdShow(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (adunit, info) => OnAdClicked(AdType.Interstitial, adunit);

                    initializedAdTypes.Add(AdType.Interstitial);
                }

                if (!string.IsNullOrEmpty(rewardedId))
                {
                    MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adunit, info) => OnAdLoaded(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adunit, error) => OnAdFailedToLoad(AdType.Rewarded, adunit, error);
                    MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adunit, error, info) => OnAdFailedToShow(AdType.Rewarded, adunit, error);
                    MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adunit, info) => OnAdShow(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (adunit, info) => OnAdClicked(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adunit, info) => OnAdClose(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adunit, reward, info) => OnAdEarnedReward(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adunit, info) => OnAdPaid(AdType.Rewarded, adunit, info);

                    initializedAdTypes.Add(AdType.Rewarded);
                }

                if (!string.IsNullOrEmpty(rewardedInterstitialAdUnitId))
                {
                    MaxSdkCallbacks.RewardedInterstitial.OnAdLoadedEvent += (adunit, info) => OnAdLoaded(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdLoadFailedEvent += (adunit, error) => OnAdFailedToLoad(AdType.RewardedInterstitial, adunit, error);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayFailedEvent += (adunit, error, info) => OnAdFailedToShow(AdType.RewardedInterstitial, adunit, error);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayedEvent += (adunit, info) => OnAdShow(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdClickedEvent += (adunit, info) => OnAdClicked(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdHiddenEvent += (adunit, info) => OnAdClose(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdReceivedRewardEvent += (adunit, reward, info) => OnAdEarnedReward(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdRevenuePaidEvent += (adunit, info) => OnAdPaid(AdType.RewardedInterstitial, adunit, info);

                    initializedAdTypes.Add(AdType.RewardedInterstitial);
                }

                if (!string.IsNullOrEmpty(mRecId))
                {
                    MaxSdkCallbacks.MRec.OnAdLoadedEvent += (adunit, info) => OnAdLoaded(AdType.MRec, adunit);
                    MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += (adunit, error) => OnAdFailedToLoad(AdType.MRec, adunit, error);
                    MaxSdkCallbacks.MRec.OnAdClickedEvent += (adunit, info) => OnAdClicked(AdType.MRec, adunit);
                    MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adunit, info) => OnAdPaid(AdType.MRec, adunit, info);

                    initializedAdTypes.Add(AdType.MRec);
                }

                RequestAll();

                // MRECs are automatically sized to 300x250.
                //MaxSdk.CreateMRec(mRecAdUnitId, MaxSdkBase.AdViewPosition.BottomCenter);

                // Initialize Adjust SDK
                //AdjustConfig adjustConfig = new AdjustConfig("YourAppToken", AdjustEnvironment.Sandbox);
                //Adjust.start(adjustConfig);

                IsInitialized = true;
                OnInitialize?.Invoke();
            };

            MaxSdk.SetSdkKey(maxSdkKey);
            MaxSdk.InitializeSdk();
            MaxSdk.CreateBanner(bannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(bannerAdUnitId, BannerColor);
        }

        public void ShowMediationDebugger()
        {
            MaxSdk.ShowMediationDebugger();
        }

        public void Request(AdType type)
        {
            if (!initializedAdTypes.Contains(type))
                return;

            if (requestingAdTypes.Contains(type))
                return;

            requestingAdTypes.Add(type);

            switch (type)
            {
                case AdType.Banner:
                    MaxSdk.LoadBanner(bannerAdUnitId);
                    break;
                case AdType.Interstitial:
                    MaxSdk.LoadInterstitial(interstitialAdUnitId);
                    break;
                case AdType.RewardedInterstitial:
                    MaxSdk.LoadRewardedInterstitialAd(rewardedInterstitialAdUnitId);
                    break;
                case AdType.Rewarded:
                    MaxSdk.LoadRewardedAd(rewardedAdUnitId);
                    break;
                case AdType.MRec:
                    MaxSdk.LoadMRec(mRecAdUnitId);
                    break;
            }

        }

        public void RequestAll()
        {
            foreach (var t in (AdType[])Enum.GetValues(typeof(AdType)))
                Request(t);
        }

        public void Show(AdType type)
        {
            if (!initializedAdTypes.Contains(type))
                return;

            if (IsReady(type))
            {
                switch (type)
                {
                    case AdType.Banner:
                        MaxSdk.ShowBanner(bannerAdUnitId);
                        break;
                    case AdType.Interstitial:
                        MaxSdk.ShowInterstitial(interstitialAdUnitId);
                        break;
                    case AdType.RewardedInterstitial:
                        MaxSdk.ShowRewardedInterstitialAd(rewardedInterstitialAdUnitId);
                        break;
                    case AdType.Rewarded:
                        MaxSdk.ShowRewardedAd(rewardedAdUnitId);
                        break;
                    case AdType.MRec:
                        MaxSdk.ShowMRec(mRecAdUnitId);
                        break;
                }
            }
        }

        public void Close(AdType type)
        {
            if (!initializedAdTypes.Contains(type))
                return;

            switch (type)
            {
                case AdType.Banner:
                    MaxSdk.HideBanner(bannerAdUnitId);
                    break;
                case AdType.MRec:
                    MaxSdk.HideMRec(mRecAdUnitId);
                    break;
            }
        }

        private void OnAdLoaded(AdType type, string adUnit)
        {
            requestingAdTypes.Remove(type);

            Debug.Log($"OnAdLoaded {type} {adUnit}");
            OnLoad?.Invoke(type, adUnit);
        }

        private void OnAdFailedToLoad(AdType type, string adUnit, MaxSdk.ErrorInfo error)
        {
            requestingAdTypes.Remove(type);

            string message = "Load Message: " + error.Message + "Code: " + error.Code;
            Debug.LogWarning($"OnAdFailedToLoad {type} {adUnit} Code: {error.Code} Message: {error.Message} " +
                $"MediatedNetworkErrorCode: {error.MediatedNetworkErrorCode} MediatedNetworkErrorMessage: {error.MediatedNetworkErrorMessage}");

            OnError?.Invoke(type, adUnit, message);
        }

        private void OnAdFailedToShow(AdType type, string adUnit, MaxSdk.ErrorInfo error)
        {
            string message = "Show Message: " + error.Message + "Code: " + error.Code;

            Debug.LogWarning($"OnAdFailedToShow {type} {adUnit} Code: {error.Code} Message: {error.Message} " +
                $"MediatedNetworkErrorCode: {error.MediatedNetworkErrorCode} MediatedNetworkErrorMessage: {error.MediatedNetworkErrorMessage}");

            OnError?.Invoke(type, adUnit, message);
        }

        private void OnAdShow(AdType type, string adUnit)
        {
            Debug.Log($"OnAdShow {type} {adUnit}");
            OnShow?.Invoke(type, adUnit);
        }

        private void OnAdClose(AdType type, string adUnit)
        {
            Debug.Log($"OnAdClose {type} {adUnit}");
            Request(type);
            OnClose?.Invoke(type, adUnit);
        }

        private void OnAdClicked(AdType type, string adUnit)
        {
            Debug.Log($"OnAdClicked {type} {adUnit}");
        }

        private void OnAdEarnedReward(AdType type, string adUnit)
        {
            Debug.Log($"OnAdearnedReward {type} {adUnit}");
            OnEarnReward?.Invoke(type, adUnit);
        }

        private void OnAdPaid(AdType type, string adUnit, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log($"OnAdPaid {type} {adUnit}");

            // Ad revenue
            double revenue = adInfo.Revenue;

            // Miscellaneous data
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD" in most cases!
            string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
            string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
            string placement = adInfo.Placement; // The placement this ad's postbacks are tied to

            OnPaid?.Invoke(type, adUnit, new Revenue
            {
                id = adUnit,
                placement = placement,
                type = type,
                countryCode = countryCode,
                value = revenue,
                currencyCode = "USD"
            });

            //AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);
            //
            //adjustAdRevenue.setRevenue(adInfo.Revenue, "USD");
            //adjustAdRevenue.setAdRevenueNetwork(adInfo.NetworkName);
            //adjustAdRevenue.setAdRevenueUnit(adInfo.AdUnitIdentifier);
            //adjustAdRevenue.setAdRevenuePlacement(adInfo.Placement);
            //
            //Adjust.trackAdRevenue(adjustAdRevenue);
        }

        [Serializable]
        public struct Revenue
        {
            public string id;
            public string placement;
            public AdType type;
            public string countryCode;
            public string network;
            public double value;
            public string currencyCode;
        }
    }
}
#endif