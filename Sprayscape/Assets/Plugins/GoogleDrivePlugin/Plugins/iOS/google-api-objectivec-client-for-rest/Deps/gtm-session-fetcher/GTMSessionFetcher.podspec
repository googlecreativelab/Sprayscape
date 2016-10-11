# This file specifies the Pod setup for GTMSessionFetcher. It enables developers
# to import GTMSessionFetcher via the CocoaPods dependency Manager.
Pod::Spec.new do |s|
  s.name        = 'GTMSessionFetcher'
  s.version     = '1.1.4'
  s.authors     = 'Google Inc.'
  s.license     = { :type => 'Apache', :file => 'LICENSE' }
  s.homepage    = 'https://github.com/google/gtm-session-fetcher'
  s.source      = { :git => 'https://github.com/google/gtm-session-fetcher.git',
                    :tag => "v#{s.version}" }
  s.summary     = 'Google Toolbox for Mac - Session Fetcher'
  s.description = <<-DESC

  GTMSessionFetcher makes it easy for Cocoa applications
  to perform http operations. The fetcher is implemented
  as a wrapper on NSURLSession, so its behavior is asynchronous
  and uses operating-system settings on iOS and Mac OS X.
  DESC

  s.ios.deployment_target = '7.0'
  s.osx.deployment_target = '10.8'
  s.tvos.deployment_target = '9.0'

  s.default_subspec = 'Full'

  s.subspec 'Core' do |sp|
    sp.source_files =
      'Source/GTMSessionFetcher.{h,m}',
      'Source/GTMSessionFetcherLogging.{h,m}',
      'Source/GTMSessionFetcherService.{h,m}',
      'Source/GTMSessionUploadFetcher.{h,m}'
    sp.framework = 'Security'
  end

  s.subspec 'Full' do |sp|
    sp.source_files =
        'Source/GTMGatherInputStream.{h,m}',
        'Source/GTMMIMEDocument.{h,m}',
        'Source/GTMReadMonitorInputStream.{h,m}'
    sp.dependency 'GTMSessionFetcher/Core', "#{s.version}"
  end

  s.subspec 'LogView' do |sp|
    # Only relevant for iOS
    sp.platform = :ios
    sp.source_files =
      'Source/GTMSessionFetcherLogViewController.{h,m}'
    sp.dependency 'GTMSessionFetcher/Core', "#{s.version}"
  end
end
