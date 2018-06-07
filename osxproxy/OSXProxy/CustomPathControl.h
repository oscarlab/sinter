//
//  CustomPathControl.h
//  NVRDP
//
//  Created by Syed Masum Billah on 2/17/16.
//  Copyright © 2016 Stony Brook University. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "Model.h"
#import "ClientHandler.h"

@interface CustomPathControl : NSPathControl <NSPathControlDelegate>{
    int keyPressCount;
    BOOL shouldForward;
}

@property (strong, nonatomic) Model* root;
@property(weak)  ClientHandler  * sharedConnection;

-(id) initWithFrame:(NSRect)frameRect;
-(id) initWithFrame:(NSRect)frameRect model:(Model*) _model andContainer:(NSView*) container;
-(void) updadeWithRoot:(Model*) _root;
@end
