#import <AuthenticationServices/AuthenticationServices.h>

#include "Shared.h"

typedef void (*ASAuthorizationAppleIDRequestCompletionCallback)(void* instance, const char* state, const char* authorizationCode, int errorCode, const char* errorMessage);

@interface Wrapped_ASAuthorizationAppleIDProvider : NSObject<ASAuthorizationControllerPresentationContextProviding, ASAuthorizationControllerDelegate>

@property (readonly, nonatomic)ASAuthorizationAppleIDProvider* appleIDProvider;
@property (readonly, nonatomic)ASAuthorizationAppleIDRequestCompletionCallback completionCallback;

@end

@implementation Wrapped_ASAuthorizationAppleIDProvider

- (instancetype)initWithCompletionCallback:(ASAuthorizationAppleIDRequestCompletionCallback)completionCallback
{
    _appleIDProvider = [[ASAuthorizationAppleIDProvider alloc] init];
    _completionCallback = completionCallback;
    return self;
}

- (nonnull ASPresentationAnchor)presentationAnchorForAuthorizationController:(nonnull ASAuthorizationController *)controller API_AVAILABLE(ios(13.0))
{
    #if __IPHONE_OS_VERSION_MAX_ALLOWED >= 130000 || __TV_OS_VERSION_MAX_ALLOWED >= 130000
        return [[[UIApplication sharedApplication] delegate] window];
    #elif __MAC_OS_X_VERSION_MAX_ALLOWED >= 101500
        return [[NSApplication sharedApplication] mainWindow];
    #else
        return nil;
    #endif
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization {
    ASAuthorizationAppleIDCredential* credential = authorization.credential;
    NSString* state = credential.state;
    NSString* authorizationCode = [[NSString alloc] initWithData:credential.authorizationCode encoding:NSUTF8StringEncoding];

    self.completionCallback((__bridge void*)self, toString(state), toString(authorizationCode), 0, NULL);
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error {
    self.completionCallback((__bridge void*)self, NULL, NULL, (int)error.code, toString(error.localizedDescription));
}

- (void)requestAppleIdWithState:(NSString *)state
{
    ASAuthorizationAppleIDRequest* request = [_appleIDProvider createRequest];
    request.requestedOperation = ASAuthorizationOperationLogin;
    request.requestedScopes = @[ASAuthorizationScopeFullName, ASAuthorizationScopeEmail];
    request.state = state;

    ASAuthorizationController* authorizationController = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
    authorizationController.presentationContextProvider = self;
    authorizationController.delegate = self;
    [authorizationController performRequests];
}

@end

extern "C"
{
    void* Wrapped_ASAuthorizationAppleIDProvider_Init(ASAuthorizationAppleIDRequestCompletionCallback completionCallback)
    {
        Wrapped_ASAuthorizationAppleIDProvider* appleIDProvider = [[Wrapped_ASAuthorizationAppleIDProvider alloc] initWithCompletionCallback:completionCallback];
        // Look at CONTRIBUTING.md on why we use __bridge_retained here.
        return (__bridge_retained void*) appleIDProvider;
    }

    void Wrapped_ASAuthorizationAppleIDProvider_Dispose(void* appleIDProviderPtr)
    {
        // Calls CFRelease because the pointer was bridged with __bridge_retained
        // See: https://developer.apple.com/library/archive/documentation/CoreFoundation/Conceptual/CFDesignConcepts/Articles/tollFreeBridgedTypes.html
        CFRelease(appleIDProviderPtr);
    }

    void Wrapped_ASAuthorizationAppleIDProvider_RequestAppleID(void* appleIDProviderPtr, const char* stateStr)
    {
        Wrapped_ASAuthorizationAppleIDProvider* appleIDProvider = (__bridge Wrapped_ASAuthorizationAppleIDProvider*) appleIDProviderPtr;
        [appleIDProvider requestAppleIdWithState:toString(stateStr)];
    }
}
