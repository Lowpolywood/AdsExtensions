#if APPLOVINMAX
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdsExtensions.Providers
{
    public class MaxSdkProvider : AdProvider
    {
        public Color BannerColor;

        public override void Initialize(bool isTest, bool consent, string apiKey, string appOpenId = null, string bannerId = null, string interstitialId = null, string rewardedId = null, string rewardedInterstitialId = null, string mRecId = null)
        {
            base.Initialize(false, consent, apiKey, appOpenId, bannerId, interstitialId, rewardedId, rewardedInterstitialId, mRecId);

            // some
            MaxSdk.SetHasUserConsent(consent);

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");

                if (!string.IsNullOrEmpty(AppOpenAdUnitId))
                {
                    MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.AppOpen, adunit);
                    MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.AppOpen, adunit, error.Message);
                    MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += (adunit, error, info) => OnError?.Invoke(AdType.AppOpen, adunit, error.Message);
                    MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += (adunit, info) => OnClose?.Invoke(AdType.AppOpen, adunit);
                    MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.AppOpen, adunit, GetRevenue(adunit, info));
                    MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += (adunit, info) => OnShow?.Invoke(AdType.AppOpen, adunit);
                    MaxSdkCallbacks.AppOpen.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.AppOpen, adunit);
                }

                if (!string.IsNullOrEmpty(BannerAdUnitId))
                {
                    MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.Banner, adunit);
                    MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.Banner, adunit, error.Message);
                    MaxSdkCallbacks.Banner.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.Banner, adunit);
                    MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.Banner, adunit, GetRevenue(adunit, info));
                }

                if (!string.IsNullOrEmpty(InterstitialAdUnitId))
                {
                    MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.Interstitial, adunit, error.Message);
                    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adunit, error, info) => OnError?.Invoke(AdType.Interstitial, adunit, error.Message);
                    MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adunit, info) => OnClose?.Invoke(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.Interstitial, adunit, GetRevenue(adunit, info));
                    MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adunit, info) => OnShow?.Invoke(AdType.Interstitial, adunit);
                    MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.Interstitial, adunit);
                }

                if (!string.IsNullOrEmpty(RewardedAdUnitId))
                {
                    MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.Rewarded, adunit, error.Message);
                    MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adunit, error, info) => OnError?.Invoke(AdType.Rewarded, adunit, error.Message);
                    MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adunit, info) => OnShow?.Invoke(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adunit, info) => OnClose?.Invoke(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adunit, reward, info) => OnEarnReward?.Invoke(AdType.Rewarded, adunit);
                    MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.Rewarded, adunit, GetRevenue(adunit, info));
                }

                if (!string.IsNullOrEmpty(RewardedInterstitialAdUnitId))
                {
                    MaxSdkCallbacks.RewardedInterstitial.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.RewardedInterstitial, adunit, error.Message);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayFailedEvent += (adunit, error, info) => OnError?.Invoke(AdType.RewardedInterstitial, adunit, error.Message);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayedEvent += (adunit, info) => OnShow?.Invoke(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdHiddenEvent += (adunit, info) => OnClose?.Invoke(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdReceivedRewardEvent += (adunit, reward, info) => OnEarnReward?.Invoke(AdType.RewardedInterstitial, adunit);
                    MaxSdkCallbacks.RewardedInterstitial.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.RewardedInterstitial, adunit, GetRevenue(adunit, info));
                }

                if (!string.IsNullOrEmpty(MRecAdUnitId))
                {
                    MaxSdkCallbacks.MRec.OnAdLoadedEvent += (adunit, info) => OnLoad?.Invoke(AdType.MRec, adunit);
                    MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += (adunit, error) => OnError?.Invoke(AdType.MRec, adunit, error.Message);
                    MaxSdkCallbacks.MRec.OnAdClickedEvent += (adunit, info) => OnClick?.Invoke(AdType.MRec, adunit);
                    MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adunit, info) => OnPaid?.Invoke(AdType.MRec, adunit, GetRevenue(adunit, info));
                }

                // MRECs are automatically sized to 300x250.
                //MaxSdk.CreateMRec(mRecAdUnitId, MaxSdkBase.AdViewPosition.BottomCenter);

                // Initialize Adjust SDK
                //AdjustConfig adjustConfig = new AdjustConfig("YourAppToken", AdjustEnvironment.Sandbox);
                //Adjust.start(adjustConfig);

                IsInitialized = true;
                OnInitialize?.Invoke();
            };

            MaxSdk.SetSdkKey(ApiKey);
            MaxSdk.InitializeSdk();
            MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, BannerColor);
        }

        public void ShowMediationDebugger()
        {
            MaxSdk.ShowMediationDebugger();
        }

        public override bool IsReady(AdType type)
        {
            if (!InitializedAdTypes.Contains(type))
                return false;

            switch (type)
            {
                case AdType.AppOpen:
                    return MaxSdk.IsAppOpenAdReady(AppOpenAdUnitId);
                case AdType.Interstitial:
                    return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
                case AdType.RewardedInterstitial:
                    return MaxSdk.IsRewardedInterstitialAdReady(RewardedInterstitialAdUnitId);
                case AdType.Rewarded:
                    return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);

                default:
                    return true;
            }
        }

        public override void Request(AdType type)
        {
            if (!InitializedAdTypes.Contains(type))
                return;

            if (IsLoading(type))
                return;

            base.Request(type);

            switch (type)
            {
                case AdType.AppOpen:
                    MaxSdk.LoadAppOpenAd(AppOpenAdUnitId);
                    break;
                case AdType.Banner:
                    MaxSdk.LoadBanner(BannerAdUnitId);
                    break;
                case AdType.Interstitial:
                    MaxSdk.LoadInterstitial(InterstitialAdUnitId);
                    break;
                case AdType.RewardedInterstitial:
                    MaxSdk.LoadRewardedInterstitialAd(RewardedInterstitialAdUnitId);
                    break;
                case AdType.Rewarded:
                    MaxSdk.LoadRewardedAd(RewardedAdUnitId);
                    break;
                case AdType.MRec:
                    MaxSdk.LoadMRec(MRecAdUnitId);
                    break;
            }

        }

        public override void Show(AdType type)
        {
            if (!InitializedAdTypes.Contains(type))
                return;

            if (IsReady(type))
            {
                switch (type)
                {
                    case AdType.AppOpen:
                        MaxSdk.ShowAppOpenAd(AppOpenAdUnitId);
                        break;
                    case AdType.Banner:
                        MaxSdk.ShowBanner(BannerAdUnitId);
                        break;
                    case AdType.Interstitial:
                        MaxSdk.ShowInterstitial(InterstitialAdUnitId);
                        break;
                    case AdType.RewardedInterstitial:
                        MaxSdk.ShowRewardedInterstitialAd(RewardedInterstitialAdUnitId);
                        break;
                    case AdType.Rewarded:
                        MaxSdk.ShowRewardedAd(RewardedAdUnitId);
                        break;
                    case AdType.MRec:
                        MaxSdk.ShowMRec(MRecAdUnitId);
                        break;
                }
            }
        }

        public void Close(AdType type)
        {
            if (!InitializedAdTypes.Contains(type))
                return;

            switch (type)
            {
                case AdType.Banner:
                    MaxSdk.HideBanner(BannerAdUnitId);
                    break;
                case AdType.MRec:
                    MaxSdk.HideMRec(MRecAdUnitId);
                    break;
            }
        }

        private Revenue GetRevenue(string adUnit, MaxSdkBase.AdInfo adInfo)
        {
            var revenue = new Revenue();

            revenue.provider = "applovin_max";
            revenue.adUnit = adUnit;
            revenue.placement = adUnit;
            revenue.value = adInfo.Revenue;
            revenue.network = adInfo.NetworkName;
            revenue.currencyCode = MaxSdk.GetSdkConfiguration().CountryCode;

            return revenue;
        }
    }
}
#endif