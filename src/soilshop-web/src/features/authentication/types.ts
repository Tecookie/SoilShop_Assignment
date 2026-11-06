export interface User {
  id: number;
  username: string;
  displayName: string;
  role: "Admin" | "Employee";
}

export interface Login {
  token: string;
  expiresAt: string;
  user: User;
}
