Pod::Spec.new do |s|
  s.name         = 'GTMOAuth2'
  s.version      = '1.1.1'
  s.author       = 'Google Inc.'
  s.homepage     = 'https://github.com/google/gtm-oauth2'
  s.license      = { :type => 'Apache', :file => 'LICENSE' }
  s.source       = { :git => 'https://github.com/google/gtm-oauth2.git',
                     :tag => "v#{s.version}" }
  s.summary      = 'Google Toolbox for Mac - OAuth 2 Controllers'
  s.description  = <<-DESC
      The Google Toolbox for Mac OAuth 2 Controllers make it easy for Cocoa
      applications to sign in to services using OAuth 2 for authentication
      and authorization.

      This version can be used with iOS ≥ 7.0 or OS X ≥ 10.9.
                   DESC

  s.ios.deployment_target = '7.0'
  s.osx.deployment_target = '10.9'
  s.requires_arc = false

  s.source_files = 'Source/*.{h,m}'
  s.ios.source_files = 'Source/Touch/*.{h,m}'
  s.ios.resources = 'Source/Touch/*.xib'
  s.osx.source_files = 'Source/Mac/*.{h,m}'
  s.osx.resources = 'Source/Mac/*.xib'

  s.user_target_xcconfig = { 'GCC_PREPROCESSOR_DEFINITIONS' => '$(inherited) GTM_OAUTH2_USE_FRAMEWORK_IMPORTS=1' }

  s.frameworks = 'Security', 'SystemConfiguration'
  s.dependency 'GTMSessionFetcher', '~> 1.1'
end
