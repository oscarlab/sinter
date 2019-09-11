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
    AppDelegate.h
    OSXScrapper

    Created by Syed Masum Billah on 2/26/15.
*/

#import <Cocoa/Cocoa.h>
#import "ScraperServer.h"

@interface AppDelegate : NSObject <NSApplicationDelegate>

@property (strong) ScraperServer * server;

@property (assign) IBOutlet NSWindow *window;
@property (weak) IBOutlet NSTextField *passcodeTextField;
@property (weak) IBOutlet NSTextField *portTextField;
@property (weak) IBOutlet NSButton *startButton;
@property (weak) IBOutlet NSButton *stopButton;

@property (nonatomic, strong, readonly ) NSDictionary *settings;

- (IBAction)startScraper:(id)sender;
- (IBAction)StopScraper:(id)sender;


@end
