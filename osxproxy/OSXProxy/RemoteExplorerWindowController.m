//
//  RemoteExplorerWindowController.m
//  NVRDP
//
//  Created by Syed Masum Billah on 8/1/14.
//  Copyright (c) 2014 Stony Brook University. All rights reserved.
//

#import "RemoteExplorerWindowController.h"


@implementation RemoteExplorerWindowController

@synthesize txtName;


- (id)initWithWindow:(NSWindow *)window
{
    self = [super initWithWindow:window];
    if (self) {
        // Initialization code here.
    }
    return self;
}

- (void)windowDidLoad
{
    [super windowDidLoad];
    [txtName becomeFirstResponder];
    NSLog(@"Explorer loaded"); //NSLeftFacingTriangleTemplate
    
    // Implement this method to handle any initialization after your window controller's window has been loaded from its nib file.
}

//Namespace Tree Control (tree)
    // Favorites ::: (tree item)
    // Desktop
        //Document




//- (void) splitView:(NSSplitView*) splitView resizeSubviewsWithOldSize:(NSSize) oldSize
//{
//	if (splitView == mSplitView)
//	{
//		CGFloat dividerPos = NSWidth([[[splitView subviews] objectAtIndex:0] frame]);
//		CGFloat width = NSWidth([splitView frame]);
//        
//		if (dividerPos < kMinSourceListWidth)
//			dividerPos = kMinSourceListWidth;
//		if (width - dividerPos < kMinContentWidth + [splitView dividerThickness])
//			dividerPos = width - (kMinContentWidth + [splitView dividerThickness]);
//		
//		[splitView adjustSubviews];
//		[splitView setPosition:dividerPos ofDividerAtIndex:0];
//	}
//}
//
//- (CGFloat) splitView:(NSSplitView*) splitView constrainSplitPosition:(CGFloat) proposedPosition ofSubviewAt:(NSInteger) dividerIndex
//{
//	if (splitView == mSplitView)
//	{
//		CGFloat width = NSWidth([splitView frame]);
//		
//		if (ABS(kSnapSourceListWidth - proposedPosition) <= kSnapToDelta)
//			proposedPosition = kSnapSourceListWidth;
//		if (proposedPosition < kMinSourceListWidth)
//			proposedPosition = kMinSourceListWidth;
//		if (width - proposedPosition < kMinContentWidth + [splitView dividerThickness])
//			proposedPosition = width - (kMinContentWidth + [splitView dividerThickness]);
//	}
//	
//	return proposedPosition;
//}


- (IBAction)save:(id)sender {
}

- (IBAction)cancel:(id)sender {
}

- (IBAction)goBack:(id)sender {
}

- (IBAction)goForward:(id)sender {
}

- (IBAction)help:(id)sender {
}

- (IBAction)newFolder:(id)sender {
}
@end
