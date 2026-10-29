import type { Metadata } from "next";
import Link from "next/link";
import { AuthPanel } from "@/features/auth/auth-panel";
import { LoginForm } from "@/features/auth/login-form";

export const metadata: Metadata = { title: "Log in" };

export default function LoginPage() {
  return (
    <AuthPanel
      title="Log in"
      description="Pick up your household's milk record."
      footer={
        <>
          <p>
            New to PureFreshMilk?{" "}
            <Link
              href="/signup"
              className="font-semibold text-accent underline-offset-4 hover:underline"
            >
              Create an account
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
      <LoginForm />
    </AuthPanel>
  );
}
