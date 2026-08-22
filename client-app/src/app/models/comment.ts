export interface ChatComment {
  id: number;
  createdAt: Date;
  updatedAt?: Date;
  body: string;
  username: string;
  displayName: string;
  image: string;
}
