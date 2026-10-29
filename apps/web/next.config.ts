import type { NextConfig } from "next";

// Cache Components, partial prefetching and the Tailwind Turbopack loader are
// the Next.js 16.4 create-next-app defaults and are kept as generated.
const nextConfig: NextConfig = {
  poweredByHeader: false,
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;
