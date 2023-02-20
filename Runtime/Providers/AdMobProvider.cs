#if ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GoogleMobileAds.Common;
using GoogleMobileAds.Api;
using GoogleMobileAds.Api.Mediation;

#if ADMOB_ADCOLONY
using GoogleMobileAds.Api.Mediation.AdColony;
#endif

#if ADMOB_APPLOVIN
using GoogleMobileAds.Api.Mediation.AppLovin;
#endif

#if ADMOB_TAPJOY
using GoogleMobileAds.Api.Mediation.Tapjoy;
#endif

#if ADMOB_VUNGLE
using GoogleMobileAds.Api.Mediation.Vungle;
#endif

#if ADMOB_UNITYADS
using GoogleMobileAds.Api.Mediation.UnityAds;
#endif

#if ADMOB_MYTARGET
using GoogleMobileAds.Api.Mediation.MyTarget;
#endif

#if ADMOB_DTEXCHANGE
using GoogleMobileAds.Api.Mediation.DTExchange;
#endif

#if ADMOB_IRONSOURCE
using GoogleMobileAds.Api.Mediation.IronSource;
#endif

namespace AdsExtensions.Providers
{
    public class AdMobProvider : AdProvider
    {
        AppOpenAd appOpenAd;
        BannerView banner;
        InterstitialAd interstitial;
        RewardedAd rewarded;

#if ADMOB_VUNGLE
    VungleInterstitialMediationExtras vungleInterstitialExtras;
    VungleRewardedVideoMediationExtras vungleRewaredVideoExtras;
#endif

        public override void Initialize(bool isTest, bool consent, string apiKey, string appOpen, string banner, string interstitial, string rewarded, string rewardedInterstitial, string mrec)
        {
            base.Initialize(isTest, consent, null, appOpen, banner, interstitial, rewarded, null, null);

            // AdMob mediation adapters
#if ADMOB_ADCOLONY
            AdColonyAppOptions.SetGDPRRequired(true);
            AdColonyAppOptions.SetGDPRConsentString(consentEnabled ? "1" : "0");
#endif

#if ADMOB_APPLOVIN
            AppLovin.SetHasUserConsent(consentEnabled);
            //AppLovin.SetIsAgeRestrictedUser(true); if user age defined
            AppLovin.SetDoNotSell(true); // Do not sell CCPA
#endif

#if ADMOB_TAPJOY
            // Y = YES, N = No, – = Not Applicable 
            //For users where CCPA doesn't apply, the string's value will always be "1---".
            Tapjoy.SetUSPrivacy("1---");
#endif

#if ADMOB_VUNGLE
            // Send placements to Vungle SDK
            var interstitials = new List<string>();
            var rewardedVideos = new List<string>();

            foreach (var p in placements)
            {
                if (p.type == Placement.Type.Interstitial)
                    interstitials.Add(p.Id);
                else if (p.type == Placement.Type.Rewarded)
                    rewardedVideos.Add(p.Id);

                yield return new WaitForEndOfFrame();
            }

            // Required for Vungle mediation < 3.1.0
            //vungleInterstitialExtras = new VungleInterstitialMediationExtras();
            //vungleRewaredVideoExtras = new VungleRewardedVideoMediationExtras();
            //
            //vungleInterstitialExtras.SetAllPlacements(interstitials.ToArray());
            //vungleRewaredVideoExtras.SetAllPlacements(rewardedVideos.ToArray());

            Vungle.UpdateConsentStatus(consentEnabled ? VungleConsent.ACCEPTED : VungleConsent.DENIED);
#endif

#if ADMOB_UNITYADS
            UnityAds.SetConsentMetaData("gdpr.consent", consentEnabled);
            UnityAds.SetConsentMetaData("privacy.consent", false); // Do not sell CCPA
#endif

#if ADMOB_MYTARGET
            MyTarget.SetUserConsent(consentEnabled);
            MyTarget.SetUserAgeRestricted(false);
            MyTarget.SetCCPAUserConsent(false); // Do not sell CCPA
#endif

#if ADMOB_DTEXCHANGE
            DTExchange.SetGDPRConsent(consentEnabled);
            DTExchange.SetGDPRConsentString("myGDPRConsentString");
#endif

#if ADMOB_IRONSOURCE
            IronSource.SetConsent(consentEnabled);
            IronSource.SetMetaData("do_not_sell", "true");
#endif

            //gameAnalyticsSDKName = "admob";

            RequestConfiguration requestConfiguration =
                new RequestConfiguration.Builder()
                .SetSameAppKeyEnabled(true).build();
            MobileAds.SetRequestConfiguration(requestConfiguration);

            MobileAds.SetiOSAppPauseOnBackground(true);

            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;
                OnInitialize?.Invoke();

                Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
                foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
                {
                    string className = keyValuePair.Key;
                    AdapterStatus status = keyValuePair.Value;
                    switch (status.InitializationState)
                    {
                        case AdapterState.NotReady:
                            // The adapter initialization did not complete.
                            Debug.Log("Adapter: " + className + " not ready.");
                            break;
                        case AdapterState.Ready:
                            // The adapter was successfully initialized.
                            Debug.Log("Adapter: " + className + " is initialized.");
                            break;
                    }
                }
            });
        }

        public override bool IsReady(AdType type) 
        {
            if (!InitializedAdTypes.Contains(type))
                return false;

            switch (type) 
            {
                case AdType.AppOpen:
                    return appOpenAd != null;
                case AdType.Banner:
                    return banner != null;
                case AdType.Interstitial:
                    return interstitial != null ? interstitial.CanShowAd() : false;
                case AdType.Rewarded:
                    return rewarded != null ? rewarded.CanShowAd() : false;
                default:
                    Debug.Log($"{type} ad not implemented in AdMob!");
                    return false;
            }
        }

        public override void Hide(AdType type) 
        {
            switch (type) 
            {
                case AdType.Banner:
                    if (banner == null)
                        return;

                    banner.Hide();
                    break;
                default:

                    break;
            }
        }

        public override void Destroy(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    if (banner == null)
                        return;

                    banner.Destroy();
                    break;
                default:

                    break;
            }
        }

        public override void Request(AdType type) 
        {
            if (!InitializedAdTypes.Contains(type))
                return;

            if (IsLoading(type))
                return;

            base.Request(type);

            string id = null;

            // Required for Vungle mediation < 3.1.0
            //if (type == Type.Interstitial)
            //{
            //    request = new AdRequest.Builder().AddMediationExtras(instance.vungleInterstitialExtras).Build();
            //}
            //else if (type == Type.Rewarded)
            //{
            //    request = new AdRequest.Builder().AddMediationExtras(instance.vungleRewaredVideoExtras).Build();
            //}

            AdRequest request = new AdRequest.Builder().Build();

            switch (type) 
            {
                case AdType.AppOpen:

                    id = IsTest ? "ca-app-pub-3940256099942544/3419835294" : AppOpenAdUnitId;

                    AppOpenAd.Load(id, ScreenOrientation.Portrait, request, ((ad, error) =>
                    {
                        if (error != null)
                        {
                            OnError(type, id, error.GetMessage());
                            return;
                        }

                        appOpenAd = ad;

                        appOpenAd.OnAdFullScreenContentClosed += () => OnClose?.Invoke(type, id);
                        appOpenAd.OnAdFullScreenContentFailed += (error) => OnError?.Invoke(type, id, error.GetMessage());
                        appOpenAd.OnAdFullScreenContentOpened += () => OnShow?.Invoke(type, id);
                        appOpenAd.OnAdImpressionRecorded += () => OnImpression?.Invoke(type, id);
                        appOpenAd.OnAdPaid += (adValue) => OnPaid?.Invoke(type, id, GetRevenue(id, adValue));
                    }));

                    break;
                case AdType.Banner:

                    id = IsTest ? "ca-app-pub-3940256099942544/6300978111" : BannerAdUnitId;

                    if (banner == null)
                    {
                        banner = new BannerView(id, AdSize.Banner, AdPosition.Bottom);

                        banner.OnBannerAdLoaded += () => { Hide(type); OnLoad?.Invoke(type, id); };
                        banner.OnBannerAdLoadFailed += (error) => { OnError?.Invoke(type, id, error.GetMessage()); };
                        banner.OnAdFullScreenContentOpened += () => { OnShow?.Invoke(type, id); };
                        banner.OnAdFullScreenContentClosed += () => { OnClose?.Invoke(type, id); };
                        banner.OnAdPaid += (adValue) => { OnPaid?.Invoke(type, id, GetRevenue(id, adValue)); };
                    }

                    banner.LoadAd(request);

                    break;
                case AdType.Interstitial:

                    id = IsTest ? "ca-app-pub-3940256099942544/6300978111" : InterstitialAdUnitId;

                    InterstitialAd.Load(id, request, (ad, error) =>
                    {
                        if (error != null)
                            OnError?.Invoke(type, id, error.GetMessage());
                        else
                        {
                            interstitial = ad;

                            interstitial.OnAdFullScreenContentFailed += (error) => { OnError?.Invoke(type, id, error.GetMessage()); };
                            interstitial.OnAdFullScreenContentOpened += () => { OnShow?.Invoke(type, id); };
                            interstitial.OnAdFullScreenContentClosed += () => { OnClose?.Invoke(type, id); };
                            interstitial.OnAdPaid += (adValue) => { OnPaid?.Invoke(type, id, GetRevenue(id, adValue)); };

                            OnLoad?.Invoke(type, id);
                        }
                    });

                    break;
                case AdType.Rewarded:

                    id = IsTest ? "ca-app-pub-3940256099942544/5224354917" : RewardedAdUnitId;

                    RewardedAd.Load(id, request, (ad, error) =>
                    {
                        if (error != null)
                            OnError?.Invoke(type, id, error.GetMessage());
                        else
                        {
                            rewarded = ad;

                            rewarded.OnAdFullScreenContentFailed += (error) => { OnError?.Invoke(type, id, error.GetMessage()); };
                            rewarded.OnAdFullScreenContentOpened += () => { OnShow?.Invoke(type, id); };
                            rewarded.OnAdFullScreenContentClosed += () =>
                            {
                                OnClose?.Invoke(type, id);

                                if (IsTest || Application.isEditor)
                                    OnEarnReward?.Invoke(type, id);
                            };
                            rewarded.OnAdPaid += (adValue) => { OnEarnReward?.Invoke(type, id); OnPaid?.Invoke(type, id, GetRevenue(id, adValue)); };

                            OnLoad?.Invoke(type, id);
                        }
                    });

                    break;
                default:
                    Debug.Log($"{type} ad not implemented in AdMob!");
                    break;
            }
        }

        public override void Show(AdType type) 
        {
            switch (type)
            {
                case AdType.AppOpen:
                    Debug.Log("SHOW APP OPEN");
                    appOpenAd.Show();
                    break;
                case AdType.Banner:
                    banner.Show();
                    break;
                case AdType.Interstitial:
                    interstitial.Show();
                    break;
                case AdType.Rewarded:
                    rewarded.Show((reward) =>
                    {

                    });
                    break;
            }
        }

        private Revenue GetRevenue(string adUnit, AdValue adValue) 
        {
            var revenue = new Revenue();

            revenue.provider = "admob";
            revenue.adUnit = adUnit;
            revenue.placement = adUnit;
            revenue.value = adValue.Value / 1000000f;
            revenue.currencyCode = adValue.CurrencyCode;

            return revenue;
        }
    }
}
#endif
