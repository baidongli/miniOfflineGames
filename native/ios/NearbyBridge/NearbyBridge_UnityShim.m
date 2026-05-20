//
//  NearbyBridge_UnityShim.m
//  Calls UnitySendMessage exported by Unity's main binary at runtime.
//  Declared extern, resolved at link time when this framework is loaded by
//  the Unity host app.
//

#import <Foundation/Foundation.h>

extern void UnitySendMessage(const char* gameObject, const char* method, const char* message);

void UnityFrameworkBridge_SendMessage(NSString* go, NSString* method, NSString* message) {
    if (go == nil || method == nil) return;
    UnitySendMessage([go UTF8String],
                     [method UTF8String],
                     message != nil ? [message UTF8String] : "");
}
