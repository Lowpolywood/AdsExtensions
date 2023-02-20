#if CLEVERADS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdsExtensions.Providers
{
    public class CleverAdsProvider : AdProvider
    {
        public ConsentStatus userConsent;
        public CCPAStatus userCCPAStatus;
        public IMediationManager manager;

        private bool isAppReturnEnable = false;

        IAdView bannerView;
        [SerializeField] AdSize bannerSize = AdSize.Banner;

        private bool collectBannerRevenue;

        public void Initialize(bool isTest, bool consent, string appOpen, string banner, string interstitial, string rewarded)
        {
            base.Initialize(isTest, consent, null, appOpen, banner, interstitial, rewarded, null, null);

        MobileAds.settings.userConsent = consentEnabled ? ConsentStatus.Accepted : ConsentStatus.Denied;
        //MobileAds.settings.userCCPAStatus = userCCPAStatus;
        MobileAds.settings.isExecuteEventsOnUnityThread = true;
        MobileAds.settings.analyticsCollectionEnabled = true;

        manager = MobileAds.BuildManager().Initialize();

        manager.SetAppReturnAdsEnabled(showAppOpenAd);

        // Errors
        manager.OnFailedToLoadAd += (adType, error) => { OnAdError(currentPlacement, error); };
        manager.OnInterstitialAdFailedToShow += (error) => { OnAdError(currentPlacement, error); };
        manager.OnRewardedAdFailedToShow += (error) => { OnAdError(currentPlacement, error); };

        // Revenue
        manager.OnInterstitialAdOpening += (metadata) =>
        {
            if (metadata.priceAccuracy == PriceAccuracy.Undisclosed)
                Debug.Log("Begin impression " + metadata.type + " ads with undisclosed cost from " + metadata.network);
            else
                ReportRevenue(currentPlacement.placement, metadata.cpm / 1000, "USD");
        };

        manager.OnRewardedAdOpening += (metadata) =>
        {
            if (metadata.priceAccuracy == PriceAccuracy.Undisclosed)
                Debug.Log("Begin impression " + metadata.type + " ads with undisclosed cost from " + metadata.network);
            else
                ReportRevenue(currentPlacement.placement, metadata.cpm / 1000, "USD");
        };

        manager.OnAppReturnAdShown += () => Debug.Log("App return ad shown");
        manager.OnAppReturnAdFailedToShow += (error) => Debug.LogError(error);
        manager.OnAppReturnAdClicked += () => Debug.Log("App return ad clicked");
        manager.OnAppReturnAdClosed += () => Debug.Log("App return ad closed");

        manager.OnInterstitialAdShown += () => { OnAdOpen(currentPlacement); };
        manager.OnInterstitialAdClicked += () => { OnAdClicked(currentPlacement); };
        manager.OnInterstitialAdClosed += () => { OnAdClose(currentPlacement); };

        manager.OnRewardedAdShown += () => { OnAdOpen(currentPlacement); };
        manager.OnRewardedAdClicked += () => { OnAdClicked(currentPlacement); };
        manager.OnRewardedAdClosed += () => { OnAdClose(currentPlacement); };
        manager.OnRewardedAdCompleted += () => { OnAdReward(currentPlacement); };

        RequestAll();

        Debug.Log($"CAS SDK version:{MobileAds.GetSDKVersion()}");
        }

        public override bool IsReady(AdType type)
        {
            switch (type)
            {
                case AdType.Banner:
                    CreateBanner();

                    return bannerView.isReady;
                case AdType.Interstitial:
                    return instance.manager.IsReadyAd(AdType.Interstitial);
                case AdType.Rewarded:
                    return instance.manager.IsReadyAd(AdType.Rewarded);
            }
        }

        public override void Hide(AdType type)
        {
            switch (type)
            {
                case Type.Banner:
                    bannerView.SetActive(false);
                    break;

            }
        }

        public override void Destroy(AdType type)
        {
            
        }

        public void CreateBanner()
        {
            if (bannerView == null)
            {
                bannerView = instance.manager.GetAdView(bannerSize);

                bannerView.OnLoaded += (view) =>
                {
                    collectBannerRevenue = true;
                    instance.OnAdLoaded(this);
                };

                bannerView.OnFailed += (view, error) => { instance.OnAdError(this, error.GetMessage()); };

                bannerView.OnPresented += (view, data) =>
                {
                    if (collectBannerRevenue)
                    {
                        collectBannerRevenue = false;

                        instance.OnAdOpen(this);

                        if (data.priceAccuracy == PriceAccuracy.Undisclosed)
                            Debug.Log("Begin impression " + data.type + " ads with undisclosed cost from " + data.network);
                        else
                            instance.ReportRevenue(placement, data.cpm / 1000, "USD");
                    }
                };

                bannerView.OnClicked += (view) => { instance.OnAdClicked(this); };
                bannerView.OnHidden += (view) => { instance.OnAdClose(this); };

                bannerView.SetActive(false);
            }
        }

        public override void Request(AdType type)
        {
            
        }

        public override void Show(AdType type)
        {
            switch (type)
            {
                case Type.Banner:
                    bannerView.SetActive(true);
                    break;
                case Type.Interstitial:
                    instance.manager.ShowAd(AdType.Interstitial);
                    break;
                case Type.Rewarded:
                    earnedReward = false;
                    instance.manager.ShowAd(AdType.Rewarded);
                    break;
            }
        }

        public void SetAppReturnAdsEnabled(bool enable)
        {
            manager.SetAppReturnAdsEnabled(enable);
        }
    }
}
#endif