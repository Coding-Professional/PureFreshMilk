import type { Metadata } from "next";
import Link from "next/link";
import { AuthPanel } from "@/features/auth/auth-panel";
import { SignupForm } from "@/features/auth/signup-form";

export const metadata: Metadata = { title: "Create an account" };

export default function SignupPage() {
  return (
    <AuthPanel
      title="Create an account"
      description="Start a milk record for your household."
      footer={
        <>
          <p>
            Already have an account?{" "}
            <Link
              href="/login"
              className="font-semibold text-accent underline-offset-4 hover:underline"
            >
              Log in
            </Link>
          </p>
          <p>
            Or{" "}
            <Link
              href="/app/today"
              className="font-semibold text-accent underline-offset-4 hover:underline"
            >
              try the Today screen with demo data
            </Link>
          </p>
        </>
      }
    >
      <SignupForm />
    </AuthPanel>
  );
}
