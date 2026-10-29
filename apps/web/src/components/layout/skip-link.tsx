export const MAIN_CONTENT_ID = "main";

export function SkipLink() {
  return (
    <a
      href={`#${MAIN_CONTENT_ID}`}
      className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-full focus:bg-ink focus:px-4 focus:py-2.5 focus:font-semibold focus:text-white"
    >
      Skip to content
    </a>
  );
}
