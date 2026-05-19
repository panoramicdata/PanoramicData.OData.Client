Hi, this is fixed in 10.0.81 - just published to NuGet.

The batch operation URLs are relative by design, and `PathAndQuery` throws on relative URIs. The fix resolves them against the client base URL before building the request line and `Host` header, so no changes needed on your end.

Thanks, Roland
