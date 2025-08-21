#import <AuthenticationServices/AuthenticationServices.h>

#include "Shared.h"

typedef void (*ASWebAuthenticationSessionCompletedCallback)(void* instance, const char* callbackUrl, int errorCode, const char* errorMessage);

@interface Wrapped_ASWebAuthenticationSession : NSObject<ASWebAuthenticationPresentationContextProviding>

@property (readonly, nonatomic)ASWebAuthenticationSession* session;

@end

@implementation Wrapped_ASWebAuthenticationSession

- (instancetype)initWithURL:(NSURL *)URL callbackURLScheme:(nullable NSString *)callbackURLScheme completionCallback:(ASWebAuthenticationSessionCompletedCallback)completionCallback
{
    _session = [[ASWebAuthenticationSession alloc] initWithURL:URL
                                            callbackURLScheme: callbackURLScheme
                                             completionHandler:^(NSURL * _Nullable callbackURL, NSError * _Nullable error)
    {
        completionCallback((__bridge void*)self, toString(callbackURL.absoluteString), (int)error.code, toString(error.localizedDescription));
    }];

    [_session setPresentationContextProvider:self];
    return self;
}

- (nonnull ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(nonnull ASWebAuthenticationSession *)session
{
    #if __IPHONE_OS_VERSION_MAX_ALLOWED >= 130000 || __TV_OS_VERSION_MAX_ALLOWED >= 130000
        return [[[UIApplication sharedApplication] delegate] window];
    #elif __MAC_OS_X_VERSION_MAX_ALLOWED >= 101500
        return [[NSApplication sharedApplication] mainWindow];
    #else
        return nil;
    #endif
}

@end

extern "C"
{
    Wrapped_ASWebAuthenticationSession* Wrapped_AS_ASWebAuthenticationSession_Init(
        const char* urlStr, const char* callbackURLSchemeStr, ASWebAuthenticationSessionCompletedCallback completionCallback
    )
    {
        NSURL* url = [NSURL URLWithString: toString(urlStr)];
        NSString* urlScheme = toString(callbackURLSchemeStr);
        Wrapped_ASWebAuthenticationSession* session = [[Wrapped_ASWebAuthenticationSession alloc] initWithURL:url
                                                                                            callbackURLScheme:urlScheme
                                                                                           completionCallback:completionCallback];
        return session;
    }

    bool Wrapped_AS_ASWebAuthenticationSession_Start(void* sessionPtr)
    {
        Wrapped_ASWebAuthenticationSession* session = (__bridge Wrapped_ASWebAuthenticationSession*) sessionPtr;
        BOOL started = [[session session] start];

        return started;
    }
}
