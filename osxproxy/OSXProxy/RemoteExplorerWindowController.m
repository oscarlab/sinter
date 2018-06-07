/* Copyright (C) 2014--2018 Stony Brook University
   Copyright (C) 2016--2018 The University of North Carolina at Chapel Hill

   This file is part of the Sinter Remote Desktop System.

   Sinter is dual-licensed, available under a commercial license or
   for free subject to the LGPL.  

   Sinter is free software: you can redistribute it and/or modify it
   under the terms of the GNU Lesser General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.  Sinter is distributed in the
   hope that it will be useful, but WITHOUT ANY WARRANTY; without even
   the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
   PURPOSE.  See the GNU Lesser General Public License for more details.  You
   should have received a copy of the GNU Lesser General Public License along
   with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

/*
    RemoteExplorerWindowController.m
    NVRDP
  
    Created by Syed Masum Billah on 8/1/14.
*/

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
