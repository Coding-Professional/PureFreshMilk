import type { ReactNode } from "react";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/ui/notice";

/** Frame for the login and sign-up previews. Authentication is not implemented yet. */
export function AuthPanel({
  title,
  description,
  children,
  footer,
}: {
  title: string;
  description: string;
  children: ReactNode;
  footer: ReactNode;
}) {
  return (
    <Card className="p-6 sm:p-8">
      <h1 className="text-3xl font-extrabold">{title}</h1>
      <p className="mt-1.5 text-ink-muted">{description}</p>
      <Notice className="mt-5" title="Accounts aren't available yet">
        This is a preview of the screen. The form is switched off and doesn&apos;t send anything.
      </Notice>
      <div className="mt-6">{children}</div>
      <div className="mt-6 grid gap-1 text-sm text-ink-muted">{footer}</div>
    </Card>
  );
}
