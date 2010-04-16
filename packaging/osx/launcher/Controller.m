/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */

#import "Controller.h"
#import "Settings.h"

@implementation Controller

- (void)awakeFromNib
{
	NSURL *settingsFile = [NSURL fileURLWithPath:[[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"launcher.ini"]];
	settings = [[Settings alloc] init];
	[settings loadSettingsFile:settingsFile];

	mods = [[NSArray arrayWithContentsOfFile:[[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"mods.plist"]] retain];
}

#pragma mark Main window
-(IBAction)launchApp:(id)sender
{
	NSString *modString = [[mods objectAtIndex:[modsList selectedRow]] objectForKey:@"Mods"];
	[settings setValue:modString forSetting:@"InitialMods"];
	[settings save];

	// Neither NSTask or NSWorkspace do what we want on pre-10.6 (we want *both* Info.plist and argument support)
	// so use the LaunchServices api directly
	NSString *path = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"OpenRA.app/Contents/MacOS/OpenRA"];
	NSArray *args = [NSArray arrayWithObjects:@"settings=../../../launcher.ini",nil];

	FSRef appRef;
	CFURLGetFSRef((CFURLRef)[NSURL URLWithString:path], &appRef);
	
	// Set the launch parameters
	LSApplicationParameters params;
		params.version = 0;
		params.flags = kLSLaunchDefaults;
		params.application = &appRef;
		params.asyncLaunchRefCon = NULL;
		params.environment = NULL; // CFDictionaryRef of environment variables; could be useful
		params.argv = (CFArrayRef)args;
		params.initialEvent = NULL;
	
	ProcessSerialNumber psn;
	OSStatus err = LSOpenApplication(&params, &psn);
	
	// Bring the game window to the front
	if (err == noErr)
		SetFrontProcess(&psn);

	// Close the launcher
	[NSApp terminate: nil];
}

- (NSInteger)numberOfRowsInTableView:(NSTableView *)aTableView
{
	return [mods count];
}

- (id)tableView:(NSTableView *)table
	objectValueForTableColumn:(NSTableColumn *)column
						  row:(NSInteger)row
{
	if (row >= [mods count])
		return @"";
	
	if ([[column identifier] isEqualToString:@"name"])
	{
		return [[mods objectAtIndex:row] objectForKey:@"Name"];
	}
	
	if ([[column identifier] isEqualToString:@"status"])
	{
		// Todo: get mod status
		return @"Todo";//[[mods objectAtIndex:row] objectForKey:@"Name"];
	}
	
	return @"";
}

#pragma mark Downloads sheet
-(IBAction)showDownloadSheet:(id)sender
{
	[NSApp beginSheet:downloadSheet modalForWindow:mainWindow
		modalDelegate:self didEndSelector:NULL contextInfo:nil];
}

- (IBAction)dismissDownloadSheet:(id)sender
{
	[NSApp endSheet:downloadSheet];
	[downloadSheet orderOut:self];
	[downloadSheet performClose:self];
}

- (IBAction)startDownload:(id)sender
{
	// Change the sheet items
	[downloadBar setHidden:NO];
	[abortButton setHidden:NO];
	[statusText setHidden:NO];
	[infoText setHidden:YES];
	[downloadButton setHidden:YES];
	[cancelButton setHidden:YES];
	
	// Create a request
	NSURL *remoteURL = [NSURL URLWithString:@"http://open-ra.org/packages/ra-packages.zip"];
	localDownloadPath = [NSTemporaryDirectory() stringByAppendingPathComponent:@"ra-packages.zip"];
	packageDirectory = [[[NSBundle mainBundle] resourcePath] stringByAppendingPathComponent:@"mods/ra/packages/"];
	
	NSLog(@"Downloading to %@",localDownloadPath);
    NSURLRequest *theRequest=[NSURLRequest requestWithURL:remoteURL
                                              cachePolicy:NSURLRequestUseProtocolCachePolicy
                                          timeoutInterval:60.0];
	
    // Create a download object
	currentDownload = [[NSURLDownload alloc] initWithRequest:theRequest delegate:self];
	
    if (currentDownload)
	{
        downloading = YES;
		[currentDownload setDestination:localDownloadPath allowOverwrite:YES];
		[statusText setStringValue:@"Connecting..."];
    }
	else
		[statusText setStringValue:@"Cannot connect to server"];
}

- (IBAction)stopDownload:(id)sender
{
	// Stop the download
	if (downloading)
		[currentDownload cancel]; 
	
	// Update the sheet status
	[downloadBar setHidden:YES];
	[abortButton setHidden:YES];
	[statusText setHidden:YES];
	[infoText setHidden:NO];
	[downloadButton setHidden:NO];
	[cancelButton setHidden:NO];
}

#pragma mark === Download Delegate Methods ===

- (void)download:(NSURLDownload *)download didFailWithError:(NSError *)error
{
	[download release]; downloading = NO;
	[statusText setStringValue:@"Error downloading file"];
}

- (void)downloadDidFinish:(NSURLDownload *)download
{
    [download release]; downloading = NO;
	[self extractPackages];
}

- (void)extractPackages
{
	[abortButton setEnabled:NO];
	[downloadBar setDoubleValue:0];
	[downloadBar setMaxValue:1];
	[downloadBar setIndeterminate:YES];
	[statusText setStringValue:@"Extracting..."];
	
	// TODO: Extract and copy files
}

- (void)download:(NSURLDownload *)download didReceiveResponse:(NSURLResponse *)response
{
	expectedData = [response expectedContentLength];
    if (expectedData > 0.0)
	{
		downloadedData = 0;
		[downloadBar setIndeterminate:NO];
		[downloadBar setMaxValue:expectedData];
        [downloadBar setDoubleValue:downloadedData];
    }
}

- (void)download:(NSURLDownload *)download didReceiveDataOfLength:(NSUInteger)length
{
    downloadedData += length;
    if (downloadedData >= expectedData)
	{
		[downloadBar setIndeterminate:YES];
		[statusText setStringValue:@"Downloading..."];
    }
	else
	{
		[downloadBar setDoubleValue:downloadedData];
		[statusText setStringValue:[NSString stringWithFormat:@"Downloading %.1f of %f",downloadedData,expectedData]];
	}
}

- (void) dealloc
{
	[mods release];
	[settings release];
	[super dealloc];
}


@end
